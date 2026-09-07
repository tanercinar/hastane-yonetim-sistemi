using System.Text.Json;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Domain;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Inpatient.Infrastructure;

public sealed class MedicationAdministrationService : IMedicationAdministrationService
{
    private readonly InpatientDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly IInpatientMedicationOrderValidator _medicationOrderValidator;

    public MedicationAdministrationService(
        InpatientDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        IInpatientMedicationOrderValidator medicationOrderValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _medicationOrderValidator = medicationOrderValidator
            ?? throw new ArgumentNullException(nameof(medicationOrderValidator));
    }

    public async Task<InpatientOperationResult<MedicationAdministrationDto>> ScheduleDoseAsync(
        ScheduleMedicationDto request,
        Guid scheduledByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == request.AdmissionId, cancellationToken);
        if (admission is null)
        {
            return InpatientOperationResult.NotFound<MedicationAdministrationDto>("Yatış kaydı bulunamadı.");
        }

        if (admission.Status != AdmissionStatus.Admitted && admission.Status != AdmissionStatus.Transferring)
        {
            return InpatientOperationResult.Conflict<MedicationAdministrationDto>(
                $"Yalnızca aktif yatan hastalar için ilaç dozu planlanabilir. Mevcut durum: {admission.Status}");
        }

        if (!request.PrescriptionId.HasValue
            || request.PrescriptionId.Value == Guid.Empty
            || !await _medicationOrderValidator.IsActiveOrderAsync(
                request.PrescriptionId.Value,
                admission.PatientId,
                request.MedicationName,
                request.Dose,
                request.Route,
                cancellationToken))
        {
            return InpatientOperationResult.Validation<MedicationAdministrationDto>(
                "PrescriptionId",
                "Doz yalnızca hastaya ait aktif ve imzalı bir hekim order/reçete kaleminden planlanabilir.");
        }

        var now = DateTime.UtcNow;
        MedicationAdministration medAdmin;
        try
        {
            medAdmin = MedicationAdministration.Schedule(
                Guid.NewGuid(),
                admission.Id,
                admission.PatientId,
                request.PrescriptionId,
                request.MedicationName,
                request.Dose,
                request.Route,
                request.ScheduledTimeUtc,
                now);
        }
        catch (ArgumentException ex)
        {
            return InpatientOperationResult.Validation<MedicationAdministrationDto>("ScheduleMedication", ex.Message);
        }

        _dbContext.MedicationAdministrations.Add(medAdmin);
        var saveError = await SaveWithConcurrencyHandlingAsync(cancellationToken);
        if (saveError is not null)
        {
            return saveError;
        }

        await PublishAuditAsync(
            "Inpatient.MedicationSchedule",
            medAdmin.Id.ToString(),
            "eMAR ilaç dozu planlandı",
            scheduledByUserId,
            JsonSerializer.Serialize(new
            {
                MedicationAdministrationId = medAdmin.Id,
                AdmissionId = admission.Id,
                ScheduledTimeUtc = medAdmin.ScheduledTimeUtc,
            }),
            now,
            cancellationToken);

        return InpatientOperationResult.Success(MapToDto(medAdmin));
    }

    public async Task<InpatientOperationResult<MedicationAdministrationDto>> AdministerMedicationAsync(
        Guid administrationId,
        AdministerMedicationDto request,
        Guid nurseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var medAdmin = await _dbContext.MedicationAdministrations
            .FirstOrDefaultAsync(m => m.Id == administrationId, cancellationToken);
        if (medAdmin is null)
        {
            return InpatientOperationResult.NotFound<MedicationAdministrationDto>("İlaç uygulama kaydı bulunamadı.");
        }

        if (!request.Verified5Rights)
        {
            return InpatientOperationResult.Validation<MedicationAdministrationDto>(
                "Verified5Rights", "İlaç uygulanmadan önce 5 Doğru Kuralı (Hasta, İlaç, Doz, Zaman, Yol) doğrulanmalıdır.");
        }

        var now = DateTime.UtcNow;
        try
        {
            medAdmin.Administer(nurseId, now, true, request.Notes);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return InpatientOperationResult.Conflict<MedicationAdministrationDto>(ex.Message);
        }

        var saveError = await SaveWithConcurrencyHandlingAsync(cancellationToken);
        if (saveError is not null)
        {
            return saveError;
        }

        await PublishAuditAsync(
            "Inpatient.MedicationAdminister",
            medAdmin.Id.ToString(),
            "eMAR ilaç uygulaması gerçekleştirildi",
            nurseId,
            JsonSerializer.Serialize(new
            {
                MedicationAdministrationId = medAdmin.Id,
                AdministeredAtUtc = medAdmin.AdministeredAtUtc,
            }),
            now,
            cancellationToken);

        return InpatientOperationResult.Success(MapToDto(medAdmin));
    }

    public async Task<InpatientOperationResult<MedicationAdministrationDto>> SkipMedicationAsync(
        Guid administrationId,
        SkipMedicationDto request,
        Guid nurseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var medAdmin = await _dbContext.MedicationAdministrations
            .FirstOrDefaultAsync(m => m.Id == administrationId, cancellationToken);
        if (medAdmin is null)
        {
            return InpatientOperationResult.NotFound<MedicationAdministrationDto>("İlaç uygulama kaydı bulunamadı.");
        }

        var now = DateTime.UtcNow;
        try
        {
            medAdmin.Skip(nurseId, request.Reason, now);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return InpatientOperationResult.Conflict<MedicationAdministrationDto>(ex.Message);
        }

        var saveError = await SaveWithConcurrencyHandlingAsync(cancellationToken);
        if (saveError is not null)
        {
            return saveError;
        }

        await PublishAuditAsync(
            "Inpatient.MedicationSkip",
            medAdmin.Id.ToString(),
            "eMAR ilaç dozu atlandı",
            nurseId,
            JsonSerializer.Serialize(new
            {
                MedicationAdministrationId = medAdmin.Id,
                Status = medAdmin.Status.ToString(),
            }),
            now,
            cancellationToken);

        return InpatientOperationResult.Success(MapToDto(medAdmin));
    }

    public async Task<InpatientOperationResult<MedicationAdministrationDto>> RefuseMedicationAsync(
        Guid administrationId,
        RefuseMedicationDto request,
        Guid nurseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var medAdmin = await _dbContext.MedicationAdministrations
            .FirstOrDefaultAsync(m => m.Id == administrationId, cancellationToken);
        if (medAdmin is null)
        {
            return InpatientOperationResult.NotFound<MedicationAdministrationDto>("İlaç uygulama kaydı bulunamadı.");
        }

        var now = DateTime.UtcNow;
        try
        {
            medAdmin.Refuse(nurseId, request.Reason, now);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return InpatientOperationResult.Conflict<MedicationAdministrationDto>(ex.Message);
        }

        var saveError = await SaveWithConcurrencyHandlingAsync(cancellationToken);
        if (saveError is not null)
        {
            return saveError;
        }

        await PublishAuditAsync(
            "Inpatient.MedicationRefuse",
            medAdmin.Id.ToString(),
            "eMAR hasta ilacı reddetti",
            nurseId,
            JsonSerializer.Serialize(new
            {
                MedicationAdministrationId = medAdmin.Id,
                Status = medAdmin.Status.ToString(),
            }),
            now,
            cancellationToken);

        return InpatientOperationResult.Success(MapToDto(medAdmin));
    }

    public async Task<InpatientOperationResult<MedicationAdministrationDto>> DelayMedicationAsync(
        Guid administrationId,
        DelayMedicationDto request,
        Guid nurseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var medAdmin = await _dbContext.MedicationAdministrations
            .FirstOrDefaultAsync(m => m.Id == administrationId, cancellationToken);
        if (medAdmin is null)
        {
            return InpatientOperationResult.NotFound<MedicationAdministrationDto>("İlaç uygulama kaydı bulunamadı.");
        }

        var now = DateTime.UtcNow;
        try
        {
            medAdmin.Delay(nurseId, request.NewScheduledTimeUtc, request.Reason, now);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return InpatientOperationResult.Conflict<MedicationAdministrationDto>(ex.Message);
        }

        var saveError = await SaveWithConcurrencyHandlingAsync(cancellationToken);
        if (saveError is not null)
        {
            return saveError;
        }

        await PublishAuditAsync(
            "Inpatient.MedicationDelay",
            medAdmin.Id.ToString(),
            "eMAR ilaç dozu ertelendi",
            nurseId,
            JsonSerializer.Serialize(new
            {
                MedicationAdministrationId = medAdmin.Id,
                NewScheduledTimeUtc = medAdmin.ScheduledTimeUtc,
                Status = medAdmin.Status.ToString(),
            }),
            now,
            cancellationToken);

        return InpatientOperationResult.Success(MapToDto(medAdmin));
    }

    public async Task<List<MedicationAdministrationDto>> GetAdministrationsByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.MedicationAdministrations
            .AsNoTracking()
            .Where(m => m.AdmissionId == admissionId)
            .OrderBy(m => m.ScheduledTimeUtc)
            .ToListAsync(cancellationToken);

        return items.Select(MapToDto).ToList();
    }

    public async Task<List<MedicationAdministrationDto>> GetDueAdministrationsAsync(
        Guid? admissionId = null,
        Guid? wardId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MedicationAdministrations
            .AsNoTracking()
            .Where(m => m.Status == MedicationAdministrationStatus.Scheduled || m.Status == MedicationAdministrationStatus.Delayed);

        if (admissionId.HasValue && admissionId.Value != Guid.Empty)
        {
            query = query.Where(m => m.AdmissionId == admissionId.Value);
        }
        else if (wardId.HasValue && wardId.Value != Guid.Empty)
        {
            var admissionIds = await _dbContext.Admissions
                .Where(a => a.AdmittingWardId == wardId.Value &&
                    (a.Status == AdmissionStatus.Admitted || a.Status == AdmissionStatus.Transferring))
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(m => admissionIds.Contains(m.AdmissionId));
        }

        var items = await query
            .OrderBy(m => m.ScheduledTimeUtc)
            .ToListAsync(cancellationToken);

        return items.Select(MapToDto).ToList();
    }

    private static MedicationAdministrationDto MapToDto(MedicationAdministration m) =>
        new(
            m.Id,
            m.AdmissionId,
            m.PatientId,
            m.PrescriptionId,
            m.MedicationName,
            m.Dose,
            m.Route,
            m.ScheduledTimeUtc,
            m.Status,
            m.AdministeredByNurseId,
            m.AdministeredAtUtc,
            m.Verified5Rights,
            m.Reason,
            m.Notes,
            m.CreatedAtUtc,
            m.Version);

    private async Task<InpatientOperationResult<MedicationAdministrationDto>?> SaveWithConcurrencyHandlingAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return InpatientOperationResult.Conflict<MedicationAdministrationDto>(
                "İlaç uygulama kaydı başka bir eşzamanlı işlem tarafından güncellendi. Kaydı yeniden yükleyiniz.");
        }
        catch (DbUpdateException)
        {
            return InpatientOperationResult.Conflict<MedicationAdministrationDto>(
                "İlaç uygulama kaydı başka bir eşzamanlı işlemle çakıştı.");
        }
    }

    private async Task PublishAuditAsync(
        string action,
        string targetResourceId,
        string reason,
        Guid actorUserId,
        string? detailsJson,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: nowUtc,
            ActorUserId: actorUserId != Guid.Empty ? actorUserId : null,
            ActorPersonId: null,
            ActorRole: null,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: "MedicationAdministration",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: detailsJson);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
