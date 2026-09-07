using System.Text.Json;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Domain;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Inpatient.Infrastructure;

public sealed class InpatientAdmissionService : IInpatientAdmissionService
{
    private readonly InpatientDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;

    public InpatientAdmissionService(
        InpatientDbContext dbContext,
        IAuditEventPublisher auditPublisher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
    }

    public async Task<InpatientOperationResult<InpatientAdmissionDto>> RequestAdmissionAsync(
        CreateAdmissionDto request,
        Guid orderingDoctorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Hasta çakışan aktif yatış kontrolü
        var hasActiveAdmission = await _dbContext.Admissions
            .AnyAsync(a => a.PatientId == request.PatientId &&
                (a.Status == AdmissionStatus.Requested ||
                 a.Status == AdmissionStatus.Accepted ||
                 a.Status == AdmissionStatus.Admitted ||
                 a.Status == AdmissionStatus.Transferring), cancellationToken);

        if (hasActiveAdmission)
        {
            return InpatientOperationResult.Conflict<InpatientAdmissionDto>(
                "Hastanın sistemde hâlihazırda aktif veya beklemede bir yatış kaydı bulunmaktadır. Aynı anda iki aktif yatış açılamaz.");
        }

        // 2. Servis kontrolü
        var ward = await _dbContext.Wards
            .FirstOrDefaultAsync(w => w.Id == request.AdmittingWardId && w.IsActive, cancellationToken);
        if (ward is null)
        {
            return InpatientOperationResult.NotFound<InpatientAdmissionDto>("Yatış talep edilen servis bulunamadı veya pasif durumda.");
        }

        if (ward.DepartmentId != request.DepartmentId)
        {
            return InpatientOperationResult.Validation<InpatientAdmissionDto>(
                "DepartmentId",
                "Yatış bölümü ile seçilen servisin bölümü aynı olmalıdır.");
        }

        var now = DateTime.UtcNow;
        var admissionId = Guid.NewGuid();
        var admissionNumber = $"DEMO-ADM-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        var admission = InpatientAdmission.Request(
            admissionId,
            admissionNumber,
            request.PatientId,
            request.EncounterId,
            orderingDoctorId,
            request.AttendingDoctorId,
            request.DepartmentId,
            request.AdmittingWardId,
            request.AdmissionReason,
            request.DiagnosisCode,
            request.DiagnosisDescription,
            request.DietType,
            request.FallRiskScore,
            request.IsolationRequired,
            request.EstimatedStayDays,
            now);

        // 3. Eğer ilk yatak seçildiyse rezerve et
        if (request.InitialBedId.HasValue && request.InitialBedId.Value != Guid.Empty)
        {
            var bed = await _dbContext.Beds
                .FirstOrDefaultAsync(b => b.Id == request.InitialBedId.Value && b.WardId == ward.Id && b.IsActive, cancellationToken);
            if (bed is null)
            {
                return InpatientOperationResult.NotFound<InpatientAdmissionDto>("Seçilen yatak bu serviste bulunamadı veya pasif.");
            }

            if (bed.Status != BedStatus.Available)
            {
                return InpatientOperationResult.Conflict<InpatientAdmissionDto>(
                    $"Seçilen yatak ({bed.BedNumber}) müsait değil. Mevcut durum: {bed.Status}");
            }

            bed.Reserve(admission.Id, admission.PatientId, now);
            admission.AssignBedDirectly(bed.Id, now);
        }

        _dbContext.Admissions.Add(admission);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return InpatientOperationResult.Conflict<InpatientAdmissionDto>(
                "Yatış başka bir eşzamanlı işlem tarafından güncellendi. Kaydı yeniden yükleyiniz.");
        }
        catch (DbUpdateException)
        {
            return InpatientOperationResult.Conflict<InpatientAdmissionDto>(
                "Hasta için başka bir eşzamanlı aktif yatış istemi oluşturuldu.");
        }

        await PublishAuditAsync(
            "Inpatient.AdmissionRequest",
            admission.Id.ToString(),
            "Yatış istemi oluşturuldu",
            orderingDoctorId,
            JsonSerializer.Serialize(new
            {
                admission.AdmissionNumber,
                admission.AdmittingWardId,
                admission.DepartmentId,
                admission.OrderingDoctorId,
                admission.AttendingDoctorId,
            }),
            now,
            cancellationToken);

        var response = await BuildAdmissionDtoAsync(admission, cancellationToken);
        return InpatientOperationResult.Success(response!);
    }

    public async Task<InpatientOperationResult<InpatientAdmissionDto>> AcceptAdmissionAsync(
        Guid admissionId,
        Guid acceptedByUserId,
        AcceptAdmissionDto? request = null,
        CancellationToken cancellationToken = default)
    {
        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == admissionId, cancellationToken);
        if (admission is null)
        {
            return InpatientOperationResult.NotFound<InpatientAdmissionDto>("Yatış kaydı bulunamadı.");
        }

        var now = DateTime.UtcNow;
        try
        {
            admission.Accept(acceptedByUserId, now);
        }
        catch (InvalidOperationException ex)
        {
            return InpatientOperationResult.Conflict<InpatientAdmissionDto>(ex.Message);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return InpatientOperationResult.Conflict<InpatientAdmissionDto>(
                "Yatış başka bir eşzamanlı işlem tarafından kabul edildi veya güncellendi. Kaydı yeniden yükleyiniz.");
        }

        await PublishAuditAsync(
            "Inpatient.AdmissionAccept",
            admission.Id.ToString(),
            "Yatış kabul edildi",
            acceptedByUserId,
            JsonSerializer.Serialize(new
            {
                admission.AdmissionNumber,
                AcceptedByUserId = acceptedByUserId,
            }),
            now,
            cancellationToken);

        var response = await BuildAdmissionDtoAsync(admission, cancellationToken);
        return InpatientOperationResult.Success(response!);
    }

    public async Task<InpatientOperationResult<InpatientAdmissionDto>> AdmitPatientAsync(
        Guid admissionId,
        AdmitPatientDto request,
        Guid performedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == admissionId, cancellationToken);
        if (admission is null)
        {
            return InpatientOperationResult.NotFound<InpatientAdmissionDto>("Yatış kaydı bulunamadı.");
        }

        var bed = await _dbContext.Beds
            .FirstOrDefaultAsync(
                b => b.Id == request.BedId
                    && b.WardId == admission.AdmittingWardId
                    && b.IsActive,
                cancellationToken);
        if (bed is null)
        {
            return InpatientOperationResult.NotFound<InpatientAdmissionDto>(
                "Atanacak yatak yatış servisinde bulunamadı veya pasif.");
        }

        if (bed.Status != BedStatus.Available && !(bed.Status == BedStatus.Reserved && bed.CurrentAdmissionId == admission.Id))
        {
            return InpatientOperationResult.Conflict<InpatientAdmissionDto>(
                $"Yatak ({bed.BedNumber}) müsait değil. Mevcut durum: {bed.Status}");
        }

        var now = DateTime.UtcNow;

        try
        {
            bed.AssignAdmission(admission.Id, admission.PatientId, now);
            admission.Admit(bed.Id, now);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return InpatientOperationResult.Conflict<InpatientAdmissionDto>(ex.Message);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return InpatientOperationResult.Conflict<InpatientAdmissionDto>(
                "Yatak başka bir eşzamanlı yatış işlemi tarafından atandı. Güncel yatak listesini yeniden yükleyiniz.");
        }
        catch (DbUpdateException)
        {
            return InpatientOperationResult.Conflict<InpatientAdmissionDto>(
                "Yatak başka bir eşzamanlı yatış işlemi tarafından atandı. Güncel yatak listesini yeniden yükleyiniz.");
        }

        await PublishAuditAsync(
            "Inpatient.AdmissionAdmit",
            admission.Id.ToString(),
            "Hasta yatağa yerleştirildi ve yatış aktif edildi",
            performedByUserId,
            JsonSerializer.Serialize(new
            {
                admission.AdmissionNumber,
                BedId = bed.Id,
                PerformedByUserId = performedByUserId,
            }),
            now,
            cancellationToken);

        var response = await BuildAdmissionDtoAsync(admission, cancellationToken);
        return InpatientOperationResult.Success(response!);
    }

    public async Task<InpatientOperationResult<InpatientAdmissionDto>> CancelAdmissionAsync(
        Guid admissionId,
        CancelAdmissionDto request,
        Guid performedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == admissionId, cancellationToken);
        if (admission is null)
        {
            return InpatientOperationResult.NotFound<InpatientAdmissionDto>("Yatış kaydı bulunamadı.");
        }

        var now = DateTime.UtcNow;

        // Eğer yatak atanmışsa serbest bırak
        if (admission.AssignedBedId.HasValue)
        {
            var bed = await _dbContext.Beds
                .FirstOrDefaultAsync(b => b.Id == admission.AssignedBedId.Value, cancellationToken);
            if (bed is not null)
            {
                if (bed.Status == BedStatus.Reserved)
                {
                    bed.CancelReservation(now);
                }
                else if (bed.Status == BedStatus.Occupied && bed.CurrentAdmissionId == admission.Id)
                {
                    bed.ReleaseBed(now, requireCleaning: false);
                }
            }
            admission.ClearBed();
        }

        try
        {
            admission.Cancel(request.Reason, now);
        }
        catch (InvalidOperationException ex)
        {
            return InpatientOperationResult.Conflict<InpatientAdmissionDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Inpatient.AdmissionCancel",
            admission.Id.ToString(),
            "Yatış istemi iptal edildi",
            performedByUserId,
            JsonSerializer.Serialize(new
            {
                admission.AdmissionNumber,
                PerformedByUserId = performedByUserId,
            }),
            now,
            cancellationToken);

        var response = await BuildAdmissionDtoAsync(admission, cancellationToken);
        return InpatientOperationResult.Success(response!);
    }

    public async Task<InpatientOperationResult<InpatientAdmissionDto>> UpdateCareDetailsAsync(
        Guid admissionId,
        UpdateCareDetailsDto request,
        Guid performedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == admissionId, cancellationToken);
        if (admission is null)
        {
            return InpatientOperationResult.NotFound<InpatientAdmissionDto>("Yatış kaydı bulunamadı.");
        }

        var now = DateTime.UtcNow;

        try
        {
            admission.UpdateCareDetails(
                request.AttendingDoctorId,
                request.DietType,
                request.FallRiskScore,
                request.IsolationRequired,
                now);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return InpatientOperationResult.Conflict<InpatientAdmissionDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Inpatient.AdmissionCareDetailsUpdate",
            admission.Id.ToString(),
            "Yatan hasta bakım detayları güncellendi",
            performedByUserId,
            JsonSerializer.Serialize(new
            {
                admission.AdmissionNumber,
                request.AttendingDoctorId,
                PerformedByUserId = performedByUserId,
            }),
            now,
            cancellationToken);

        var response = await BuildAdmissionDtoAsync(admission, cancellationToken);
        return InpatientOperationResult.Success(response!);
    }

    public async Task<InpatientAdmissionDto?> GetAdmissionByIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        var admission = await _dbContext.Admissions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == admissionId, cancellationToken);

        if (admission is null)
        {
            return null;
        }

        return await BuildAdmissionDtoAsync(admission, cancellationToken);
    }

    public async Task<InpatientAdmissionDto?> GetActiveAdmissionByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var admission = await _dbContext.Admissions
            .AsNoTracking()
            .Where(a => a.PatientId == patientId &&
                (a.Status == AdmissionStatus.Requested ||
                 a.Status == AdmissionStatus.Accepted ||
                 a.Status == AdmissionStatus.Admitted ||
                 a.Status == AdmissionStatus.Transferring))
            .OrderByDescending(a => a.RequestedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (admission is null)
        {
            return null;
        }

        return await BuildAdmissionDtoAsync(admission, cancellationToken);
    }

    public async Task<List<InpatientAdmissionSummaryDto>> GetAdmissionsAsync(
        Guid? wardId = null,
        Guid? departmentId = null,
        string? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Admissions.AsNoTracking();

        if (wardId.HasValue && wardId.Value != Guid.Empty)
        {
            query = query.Where(a => a.AdmittingWardId == wardId.Value);
        }

        if (departmentId.HasValue && departmentId.Value != Guid.Empty)
        {
            query = query.Where(a => a.DepartmentId == departmentId.Value);
        }

        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            query = query.Where(a => a.PatientId == patientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<AdmissionStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(a => a.Status == parsedStatus);
        }

        var admissions = await query
            .OrderByDescending(a => a.RequestedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var wardIds = admissions.Select(a => a.AdmittingWardId).Distinct().ToList();
        var bedIds = admissions.Where(a => a.AssignedBedId.HasValue).Select(a => a.AssignedBedId!.Value).Distinct().ToList();

        var wards = await _dbContext.Wards
            .AsNoTracking()
            .Where(w => wardIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, cancellationToken);

        var beds = await _dbContext.Beds
            .AsNoTracking()
            .Where(b => bedIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        var roomIds = beds.Values.Select(b => b.RoomId).Distinct().ToList();
        var rooms = await _dbContext.Rooms
            .AsNoTracking()
            .Where(r => roomIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.RoomNumber, cancellationToken);

        return admissions.Select(a =>
        {
            var wardName = wards.GetValueOrDefault(a.AdmittingWardId, "Bilinmeyen Servis");
            beds.TryGetValue(a.AssignedBedId ?? Guid.Empty, out var bed);
            string? roomNumber = null;
            if (bed is not null)
            {
                rooms.TryGetValue(bed.RoomId, out roomNumber);
            }

            return new InpatientAdmissionSummaryDto(
                a.Id,
                a.AdmissionNumber,
                a.PatientId,
                a.OrderingDoctorId,
                a.AttendingDoctorId,
                a.DepartmentId,
                a.AdmittingWardId,
                wardName,
                a.AssignedBedId,
                bed?.BedNumber,
                roomNumber,
                a.Status,
                a.AdmissionReason,
                a.DietType,
                a.FallRiskScore,
                a.IsolationRequired,
                a.RequestedAtUtc,
                a.AdmittedAtUtc);
        }).ToList();
    }

    private async Task<InpatientAdmissionDto?> BuildAdmissionDtoAsync(
        InpatientAdmission admission,
        CancellationToken cancellationToken)
    {
        var ward = await _dbContext.Wards
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == admission.AdmittingWardId, cancellationToken);

        Bed? bed = null;
        string? roomNumber = null;
        if (admission.AssignedBedId.HasValue)
        {
            bed = await _dbContext.Beds
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == admission.AssignedBedId.Value, cancellationToken);
            if (bed is not null)
            {
                var room = await _dbContext.Rooms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == bed.RoomId, cancellationToken);
                roomNumber = room?.RoomNumber;
            }
        }

        return new InpatientAdmissionDto(
            admission.Id,
            admission.AdmissionNumber,
            admission.PatientId,
            admission.EncounterId,
            admission.OrderingDoctorId,
            admission.AttendingDoctorId,
            admission.DepartmentId,
            admission.AdmittingWardId,
            ward?.Name ?? "Bilinmeyen Servis",
            admission.AssignedBedId,
            bed?.BedNumber,
            roomNumber,
            admission.Status,
            admission.AdmissionReason,
            admission.DiagnosisCode,
            admission.DiagnosisDescription,
            admission.DietType,
            admission.FallRiskScore,
            admission.IsolationRequired,
            admission.EstimatedStayDays,
            admission.RequestedAtUtc,
            admission.AcceptedAtUtc,
            admission.AdmittedAtUtc,
            admission.DischargedAtUtc,
            admission.DischargeSummary,
            admission.CancelledAtUtc,
            admission.CancellationReason,
            admission.Version);
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
            TargetResourceType: "InpatientAdmission",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: detailsJson);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
