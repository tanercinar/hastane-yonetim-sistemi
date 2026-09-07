using System.Text.Json;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.Emergency.Application;
using HospitalManagement.Modules.Emergency.Domain;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Emergency.Infrastructure;

public sealed class EmergencyAdmissionService : IEmergencyAdmissionService
{
    private static readonly EmergencyAdmissionStatus[] ActiveStatuses =
    [
        EmergencyAdmissionStatus.WaitingTriage,
        EmergencyAdmissionStatus.TriagedWaitingDoctor,
        EmergencyAdmissionStatus.InEvaluation,
        EmergencyAdmissionStatus.InObservation,
    ];

    private readonly EmergencyDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly IEmergencyRealtimeNotifier _notifier;
    private readonly TimeProvider _timeProvider;

    public EmergencyAdmissionService(
        EmergencyDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        IEmergencyRealtimeNotifier notifier,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<EmergencyOperationResult<EmergencyAdmissionDto>> CreateAdmissionAsync(
        CreateEmergencyAdmissionDto request,
        Guid admittingStaffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PatientId == Guid.Empty)
        {
            return EmergencyOperationResult.Validation<EmergencyAdmissionDto>("PatientId", "Hasta seçilmelidir.");
        }

        if (string.IsNullOrWhiteSpace(request.ChiefComplaint))
        {
            return EmergencyOperationResult.Validation<EmergencyAdmissionDto>("ChiefComplaint", "Başvuru şikâyeti boş olamaz.");
        }

        // Invariant: One active emergency admission per patient
        var hasActiveAdmission = await _dbContext.Admissions
            .AnyAsync(a => a.PatientId == request.PatientId && ActiveStatuses.Contains(a.Status), cancellationToken);

        if (hasActiveAdmission)
        {
            return EmergencyOperationResult.Conflict<EmergencyAdmissionDto>(
                "Hastanın halihazırda aktif bir acil başvurusu bulunmaktadır.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var admission = EmergencyAdmission.Create(
            Guid.NewGuid(),
            request.PatientId,
            request.ArrivalType,
            request.ChiefComplaint,
            request.AdmissionNotes,
            admittingStaffId,
            now);

        _dbContext.Admissions.Add(admission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Emergency.AdmissionCreate",
            admission.Id.ToString(),
            "Yeni acil başvurusu kaydedildi",
            admittingStaffId,
            JsonSerializer.Serialize(new
            {
                AdmissionId = admission.Id,
                ArrivalType = admission.ArrivalType.ToString(),
            }),
            now,
            cancellationToken);

        await _notifier.NotifyAdmissionCreatedAsync(
            admission.Id,
            admission.EmergencyProtocolNumber,
            admission.ArrivalType.ToString(),
            cancellationToken);

        return EmergencyOperationResult.Success(MapToDto(admission));
    }

    public async Task<EmergencyOperationResult<EmergencyAdmissionDto>> RecordTriageAsync(
        Guid id,
        RecordTriageDto request,
        Guid triageNurseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TriageCategoryReason))
        {
            return EmergencyOperationResult.Validation<EmergencyAdmissionDto>(
                "TriageCategoryReason", "Triyaj gerekçesi boş bırakılamaz.");
        }

        if (request.PainScale.HasValue && (request.PainScale.Value < 0 || request.PainScale.Value > 10))
        {
            return EmergencyOperationResult.Validation<EmergencyAdmissionDto>(
                "PainScale", "Ağrı skalası 0 ile 10 arasında olmalıdır.");
        }

        if (request.OxygenSaturationPercent.HasValue && (request.OxygenSaturationPercent.Value < 0 || request.OxygenSaturationPercent.Value > 100))
        {
            return EmergencyOperationResult.Validation<EmergencyAdmissionDto>(
                "OxygenSaturationPercent", "Oksijen satürasyonu 0 ile 100 arasında olmalıdır.");
        }

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (admission is null)
        {
            return EmergencyOperationResult.NotFound<EmergencyAdmissionDto>("Acil başvurusu bulunamadı.");
        }

        if (!ActiveStatuses.Contains(admission.Status))
        {
            return EmergencyOperationResult.Conflict<EmergencyAdmissionDto>(
                $"Sonuçlandırılmış başvuruya ({admission.Status}) triyaj uygulanamaz.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            admission.RecordTriage(
                request.TriageLevel,
                request.TriageCategoryReason,
                triageNurseId,
                request.SystolicBp,
                request.DiastolicBp,
                request.HeartRate,
                request.BodyTemperatureCelsius,
                request.RespiratoryRate,
                request.OxygenSaturationPercent,
                request.PainScale,
                request.Consciousness,
                request.ClinicalNotes,
                now);
        }
        catch (Exception ex)
        {
            return EmergencyOperationResult.Validation<EmergencyAdmissionDto>("Triage", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Emergency.TriageRecord",
            admission.Id.ToString(),
            "Acil triyaj kaydı girildi",
            triageNurseId,
            JsonSerializer.Serialize(new
            {
                AdmissionId = admission.Id,
                TriageLevel = request.TriageLevel.ToString(),
                EducationalClassificationAssisted = true,
            }),
            now,
            cancellationToken);

        await _notifier.NotifyTriageRecordedAsync(
            admission.Id,
            admission.EmergencyProtocolNumber,
            request.TriageLevel.ToString(),
            cancellationToken);

        return EmergencyOperationResult.Success(MapToDto(admission));
    }

    public async Task<EmergencyOperationResult<EmergencyAdmissionDto>> AssignDoctorAsync(
        Guid id,
        AssignDoctorDto request,
        Guid assignedByStaffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.DoctorId == Guid.Empty)
        {
            return EmergencyOperationResult.Validation<EmergencyAdmissionDto>("DoctorId", "Hekim seçilmelidir.");
        }

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (admission is null)
        {
            return EmergencyOperationResult.NotFound<EmergencyAdmissionDto>("Acil başvurusu bulunamadı.");
        }

        if (!ActiveStatuses.Contains(admission.Status))
        {
            return EmergencyOperationResult.Conflict<EmergencyAdmissionDto>(
                $"Sonuçlandırılmış başvuruya ({admission.Status}) hekim atanamaz.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        admission.AssignDoctor(request.DoctorId, now);
        if (!string.IsNullOrWhiteSpace(request.BedOrZone))
        {
            admission.AssignBedOrZone(request.BedOrZone, now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Emergency.DoctorAssign",
            admission.Id.ToString(),
            "Acil hastasına hekim/alan atandı",
            assignedByStaffId,
            JsonSerializer.Serialize(new
            {
                AdmissionId = admission.Id,
                AssignedDoctorId = request.DoctorId,
            }),
            now,
            cancellationToken);

        await _notifier.NotifyDoctorAssignedAsync(
            admission.Id,
            admission.EmergencyProtocolNumber,
            request.DoctorId,
            request.BedOrZone,
            cancellationToken);

        return EmergencyOperationResult.Success(MapToDto(admission));
    }

    public async Task<EmergencyOperationResult<EmergencyAdmissionDto>> UpdateStatusAsync(
        Guid id,
        UpdateEmergencyStatusDto request,
        Guid updatedByStaffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (admission is null)
        {
            return EmergencyOperationResult.NotFound<EmergencyAdmissionDto>("Acil başvurusu bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var oldStatus = admission.Status;
        admission.UpdateStatus(request.Status, request.Notes, now);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Emergency.StatusChange",
            admission.Id.ToString(),
            $"Acil başvuru durumu güncellendi: {oldStatus} -> {request.Status}",
            updatedByStaffId,
            JsonSerializer.Serialize(new
            {
                AdmissionId = admission.Id,
                OldStatus = oldStatus.ToString(),
                NewStatus = request.Status.ToString(),
            }),
            now,
            cancellationToken);

        await _notifier.NotifyStatusChangedAsync(
            admission.Id,
            admission.EmergencyProtocolNumber,
            oldStatus.ToString(),
            request.Status.ToString(),
            cancellationToken);

        return EmergencyOperationResult.Success(MapToDto(admission));
    }

    public async Task<EmergencyAdmissionDto?> GetAdmissionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var admission = await _dbContext.Admissions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return admission is null ? null : MapToDto(admission);
    }

    public async Task<EmergencyAdmissionDto?> GetActiveAdmissionByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var admission = await _dbContext.Admissions
            .AsNoTracking()
            .Where(a => a.PatientId == patientId && ActiveStatuses.Contains(a.Status))
            .OrderByDescending(a => a.AdmittedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return admission is null ? null : MapToDto(admission);
    }

    public async Task<List<EmergencyAdmissionSummaryDto>> GetAdmissionsAsync(
        EmergencyAdmissionStatus? status = null,
        TriageLevel? triageLevel = null,
        Guid? patientId = null,
        Guid? assignedDoctorId = null,
        DateTime? fromDateUtc = null,
        DateTime? toDateUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Admissions.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (triageLevel.HasValue)
        {
            query = query.Where(a => a.Triage != null && a.Triage.TriageLevel == triageLevel.Value);
        }

        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            query = query.Where(a => a.PatientId == patientId.Value);
        }

        if (assignedDoctorId.HasValue && assignedDoctorId.Value != Guid.Empty)
        {
            query = query.Where(a => a.AssignedDoctorId == assignedDoctorId.Value);
        }

        if (fromDateUtc.HasValue)
        {
            query = query.Where(a => a.AdmittedAtUtc >= fromDateUtc.Value);
        }

        if (toDateUtc.HasValue)
        {
            query = query.Where(a => a.AdmittedAtUtc <= toDateUtc.Value);
        }

        var list = await query
            .OrderByDescending(a => a.AdmittedAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToSummaryDto).ToList();
    }

    private static EmergencyAdmissionDto MapToDto(EmergencyAdmission a) =>
        new(
            a.Id,
            a.EmergencyProtocolNumber,
            a.PatientId,
            a.ArrivalType,
            a.ChiefComplaint,
            a.AdmissionNotes,
            a.Status,
            a.AdmittedAtUtc,
            a.AdmittingStaffId,
            a.Triage is null ? null : new EmergencyTriageDto(
                a.Triage.TriageLevel,
                a.Triage.TriageCategoryReason,
                a.Triage.TriagedAtUtc,
                a.Triage.TriageNurseId,
                a.Triage.EducationalClassificationAssisted,
                a.Triage.SystolicBp,
                a.Triage.DiastolicBp,
                a.Triage.HeartRate,
                a.Triage.BodyTemperatureCelsius,
                a.Triage.RespiratoryRate,
                a.Triage.OxygenSaturationPercent,
                a.Triage.PainScale,
                a.Triage.Consciousness,
                a.Triage.ClinicalNotes),
            a.AssignedDoctorId,
            a.AssignedBedOrZone,
            a.CompletedAtUtc,
            a.DischargeOrDispositionNotes,
            a.CreatedAtUtc,
            a.UpdatedAtUtc,
            a.Version);

    private static EmergencyAdmissionSummaryDto MapToSummaryDto(EmergencyAdmission a) =>
        new(
            a.Id,
            a.EmergencyProtocolNumber,
            a.PatientId,
            a.ArrivalType,
            a.ChiefComplaint,
            a.Status,
            a.Triage?.TriageLevel,
            a.AdmittedAtUtc,
            a.AssignedDoctorId,
            a.AssignedBedOrZone);

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
            TargetResourceType: "EmergencyAdmission",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: detailsJson);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
