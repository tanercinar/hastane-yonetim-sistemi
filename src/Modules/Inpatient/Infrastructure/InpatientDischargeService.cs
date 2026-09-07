using System.Text.Json;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Domain;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Inpatient.Infrastructure;

public sealed class InpatientDischargeService : IInpatientDischargeService
{
    private readonly InpatientDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly IInpatientRealtimeNotifier _realtimeNotifier;

    public InpatientDischargeService(
        InpatientDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        IInpatientRealtimeNotifier realtimeNotifier)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _realtimeNotifier = realtimeNotifier ?? throw new ArgumentNullException(nameof(realtimeNotifier));
    }

    public async Task<InpatientOperationResult<InpatientDischargeDto>> ProcessDischargeAsync(
        DischargeAdmissionDto request,
        Guid dischargingDoctorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == request.AdmissionId, cancellationToken);
        if (admission is null)
        {
            return InpatientOperationResult.NotFound<InpatientDischargeDto>("Yatış kaydı bulunamadı.");
        }

        if (admission.Status != AdmissionStatus.Admitted && admission.Status != AdmissionStatus.Transferring)
        {
            return InpatientOperationResult.Conflict<InpatientDischargeDto>(
                $"Yalnızca yatakta aktif olan hastalar taburcu edilebilir. Mevcut durum: {admission.Status}");
        }

        var existingDischarge = await _dbContext.Discharges
            .AnyAsync(d => d.AdmissionId == request.AdmissionId, cancellationToken);
        if (existingDischarge)
        {
            return InpatientOperationResult.Conflict<InpatientDischargeDto>("Bu yatış için zaten taburculuk kaydı oluşturulmuş.");
        }

        var now = DateTime.UtcNow;
        InpatientDischarge discharge;
        try
        {
            discharge = InpatientDischarge.Create(
                Guid.NewGuid(),
                admission.Id,
                admission.PatientId,
                dischargingDoctorId,
                request.DischargeType,
                request.DischargeSummary,
                request.FinalDiagnosisCode,
                request.FinalDiagnosisDescription,
                request.DischargeRecommendations,
                request.DischargePrescriptionSummary,
                request.FollowUpAppointmentDateUtc,
                request.FollowUpDepartmentId,
                request.TransferFacilityName,
                request.TransferReason,
                now,
                now);
        }
        catch (ArgumentException ex)
        {
            return InpatientOperationResult.Validation<InpatientDischargeDto>("Discharge", ex.Message);
        }

        // Discharge admission
        admission.Discharge(request.DischargeSummary, now);

        // Free up assigned bed to cleaning
        if (admission.AssignedBedId.HasValue)
        {
            var bed = await _dbContext.Beds
                .FirstOrDefaultAsync(b => b.Id == admission.AssignedBedId.Value, cancellationToken);
            if (bed is not null)
            {
                bed.ReleaseBed(now, requireCleaning: true);
            }
            admission.ClearBed();
        }

        _dbContext.Discharges.Add(discharge);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return InpatientOperationResult.Conflict<InpatientDischargeDto>(
                "Yatış başka bir eşzamanlı işlem tarafından taburcu edildi veya güncellendi. Kaydı yeniden yükleyiniz.");
        }
        catch (DbUpdateException)
        {
            return InpatientOperationResult.Conflict<InpatientDischargeDto>(
                "Bu yatış için başka bir eşzamanlı taburculuk kaydı oluşturuldu.");
        }

        // Audit log
        await PublishAuditAsync(
            "Inpatient.AdmissionDischarge",
            discharge.Id.ToString(),
            "Hasta taburculuk işlemi tamamlandı",
            dischargingDoctorId,
            JsonSerializer.Serialize(new
            {
                DischargeId = discharge.Id,
                AdmissionId = admission.Id,
                DischargeType = discharge.DischargeType.ToString(),
                DischargedAtUtc = discharge.DischargedAtUtc,
            }),
            now,
            cancellationToken);

        if (discharge.DischargeType == DischargeType.TransferToOtherFacility)
        {
            await PublishAuditAsync(
                "Inpatient.ExternalReferralMock",
                discharge.Id.ToString(),
                "Dış sağlık kuruluşuna sevk mock kaydı oluşturuldu",
                dischargingDoctorId,
                JsonSerializer.Serialize(new
                {
                    DischargeId = discharge.Id,
                    AdmissionId = admission.Id,
                    IntegrationMode = "MOCK",
                }),
                now,
                cancellationToken);
        }

        // Realtime notification
        await _realtimeNotifier.NotifyDischargeCompletedAsync(
            discharge.Id,
            admission.Id,
            admission.AdmittingWardId,
            discharge.DischargeType.ToString(),
            cancellationToken);

        return InpatientOperationResult.Success(MapToDto(discharge));
    }

    public async Task<InpatientDischargeDto?> GetDischargeByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        var discharge = await _dbContext.Discharges
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.AdmissionId == admissionId, cancellationToken);

        return discharge is null ? null : MapToDto(discharge);
    }

    public async Task<List<InpatientDischargeDto>> GetDischargesAsync(
        Guid? patientId = null,
        DateTime? fromDateUtc = null,
        DateTime? toDateUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Discharges.AsNoTracking();

        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            query = query.Where(d => d.PatientId == patientId.Value);
        }

        if (fromDateUtc.HasValue)
        {
            query = query.Where(d => d.DischargedAtUtc >= fromDateUtc.Value);
        }

        if (toDateUtc.HasValue)
        {
            query = query.Where(d => d.DischargedAtUtc <= toDateUtc.Value);
        }

        var list = await query
            .OrderByDescending(d => d.DischargedAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    private static InpatientDischargeDto MapToDto(InpatientDischarge d) =>
        new(
            d.Id,
            d.AdmissionId,
            d.PatientId,
            d.DischargingDoctorId,
            d.DischargeType,
            d.DischargeSummary,
            d.FinalDiagnosisCode,
            d.FinalDiagnosisDescription,
            d.DischargeRecommendations,
            d.DischargePrescriptionSummary,
            d.FollowUpAppointmentDateUtc,
            d.FollowUpDepartmentId,
            d.TransferFacilityName,
            d.TransferReason,
            d.DischargedAtUtc,
            d.CreatedAtUtc,
            d.Version);

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
            TargetResourceType: "InpatientDischarge",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: detailsJson);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
