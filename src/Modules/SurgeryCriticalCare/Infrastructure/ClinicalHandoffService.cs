using System.Text.Json;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;

public sealed class ClinicalHandoffService : IClinicalHandoffService
{
    private readonly SurgeryDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly TimeProvider _timeProvider;

    public ClinicalHandoffService(
        SurgeryDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SurgeryOperationResult<ClinicalHandoffDto>> InitiateHandoffAsync(
        InitiateClinicalHandoffDto dto,
        Guid handingOverStaffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.PatientId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<ClinicalHandoffDto>("PatientId", "Hasta seçilmelidir.");
        }

        if (handingOverStaffId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<ClinicalHandoffDto>("HandingOverStaffId", "Devreden sağlık personeli belirlenemedi.");
        }

        if (string.IsNullOrWhiteSpace(dto.Situation))
        {
            return SurgeryOperationResult.Validation<ClinicalHandoffDto>("Situation", "ISBAR Durum (Situation) bilgisi zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(dto.Background))
        {
            return SurgeryOperationResult.Validation<ClinicalHandoffDto>("Background", "ISBAR Geçmiş (Background) bilgisi zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(dto.Assessment))
        {
            return SurgeryOperationResult.Validation<ClinicalHandoffDto>("Assessment", "ISBAR Değerlendirme (Assessment) bilgisi zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(dto.Recommendation))
        {
            return SurgeryOperationResult.Validation<ClinicalHandoffDto>("Recommendation", "ISBAR Öneriler (Recommendation) bilgisi zorunludur.");
        }

        var existingPending = await _dbContext.ClinicalHandoffs
            .AnyAsync(h => h.PatientId == dto.PatientId && h.Status == HandoffStatus.PendingAcceptance, cancellationToken);

        if (existingPending)
        {
            return SurgeryOperationResult.Conflict<ClinicalHandoffDto>("Bu hasta için hâlihazırda kabul bekleyen bir devir teslim süreci bulunmaktadır.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var handoff = ClinicalHandoff.Initiate(
            Guid.NewGuid(),
            dto.PatientId,
            dto.InpatientStayId,
            dto.EncounterId,
            dto.SourceArea,
            dto.SourceLocationDetails,
            dto.DestinationArea,
            dto.DestinationLocationDetails,
            handingOverStaffId,
            dto.Situation,
            dto.Background,
            dto.Assessment,
            dto.Recommendation,
            dto.CriticalAlerts,
            now);

        _dbContext.ClinicalHandoffs.Add(handoff);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.ClinicalHandoffInitiate",
            handoff.Id.ToString(),
            "Klinik devir teslim başlatıldı",
            handingOverStaffId,
            JsonSerializer.Serialize(new
            {
                HandoffId = handoff.Id,
                Source = dto.SourceArea.ToString(),
                Destination = dto.DestinationArea.ToString(),
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(MapToDto(handoff));
    }

    public async Task<SurgeryOperationResult<ClinicalHandoffDto>> AcceptHandoffAsync(
        Guid handoffId,
        Guid receivingStaffId,
        string? acceptanceNote,
        CancellationToken cancellationToken = default)
    {
        var handoff = await _dbContext.ClinicalHandoffs.FirstOrDefaultAsync(h => h.Id == handoffId, cancellationToken);
        if (handoff is null)
        {
            return SurgeryOperationResult.NotFound<ClinicalHandoffDto>("Devir teslim kaydı bulunamadı.");
        }

        if (handoff.Status != HandoffStatus.PendingAcceptance)
        {
            return SurgeryOperationResult.Conflict<ClinicalHandoffDto>("Yalnızca onay bekleyen devir teslimler kabul edilebilir.");
        }

        if (receivingStaffId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<ClinicalHandoffDto>("ReceivingStaffId", "Devralan personel bilgisi zorunludur.");
        }

        if (receivingStaffId == handoff.HandingOverStaffId)
        {
            return SurgeryOperationResult.Conflict<ClinicalHandoffDto>("Devreden personel kendi devir teslimini tek taraflı kabul edemez; teslim alan hekim/hemşire onayı gerekir.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            handoff.Accept(receivingStaffId, acceptanceNote, now);
        }
        catch (Exception ex)
        {
            return SurgeryOperationResult.Validation<ClinicalHandoffDto>("Accept", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.ClinicalHandoffAccept",
            handoff.Id.ToString(),
            "Klinik devir teslim kabul edildi",
            receivingStaffId,
            JsonSerializer.Serialize(new
            {
                HandoffId = handoff.Id,
                AcceptedBy = receivingStaffId,
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(MapToDto(handoff));
    }

    public async Task<SurgeryOperationResult<ClinicalHandoffDto>> RejectHandoffAsync(
        Guid handoffId,
        Guid rejectingStaffId,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            return SurgeryOperationResult.Validation<ClinicalHandoffDto>("RejectionReason", "Devir teslim ret gerekçesi belirtilmelidir.");
        }

        var handoff = await _dbContext.ClinicalHandoffs.FirstOrDefaultAsync(h => h.Id == handoffId, cancellationToken);
        if (handoff is null)
        {
            return SurgeryOperationResult.NotFound<ClinicalHandoffDto>("Devir teslim kaydı bulunamadı.");
        }

        if (handoff.Status != HandoffStatus.PendingAcceptance)
        {
            return SurgeryOperationResult.Conflict<ClinicalHandoffDto>("Yalnızca onay bekleyen devir teslimler reddedilebilir.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            handoff.Reject(rejectingStaffId, rejectionReason, now);
        }
        catch (Exception ex)
        {
            return SurgeryOperationResult.Validation<ClinicalHandoffDto>("Reject", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.ClinicalHandoffReject",
            handoff.Id.ToString(),
            "Klinik devir teslim reddedildi / revizyon istendi",
            rejectingStaffId,
            JsonSerializer.Serialize(new
            {
                HandoffId = handoff.Id,
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(MapToDto(handoff));
    }

    public async Task<SurgeryOperationResult<ClinicalHandoffDto>> CancelHandoffAsync(
        Guid handoffId,
        Guid cancellingStaffId,
        string cancelReason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cancelReason))
        {
            return SurgeryOperationResult.Validation<ClinicalHandoffDto>("CancelReason", "İptal gerekçesi belirtilmelidir.");
        }

        var handoff = await _dbContext.ClinicalHandoffs.FirstOrDefaultAsync(h => h.Id == handoffId, cancellationToken);
        if (handoff is null)
        {
            return SurgeryOperationResult.NotFound<ClinicalHandoffDto>("Devir teslim kaydı bulunamadı.");
        }

        if (handoff.Status != HandoffStatus.PendingAcceptance)
        {
            return SurgeryOperationResult.Conflict<ClinicalHandoffDto>("Yalnızca onay bekleyen devir teslimler iptal edilebilir.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            handoff.Cancel(cancellingStaffId, cancelReason, now);
        }
        catch (Exception ex)
        {
            return SurgeryOperationResult.Validation<ClinicalHandoffDto>("Cancel", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.ClinicalHandoffCancel",
            handoff.Id.ToString(),
            "Klinik devir teslim iptal edildi",
            cancellingStaffId,
            JsonSerializer.Serialize(new
            {
                HandoffId = handoff.Id,
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(MapToDto(handoff));
    }

    public async Task<ClinicalHandoffDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var handoff = await _dbContext.ClinicalHandoffs
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        return handoff is null ? null : MapToDto(handoff);
    }

    public async Task<List<ClinicalHandoffDto>> GetPendingHandoffsAsync(
        ClinicalAreaType? destinationArea = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ClinicalHandoffs
            .AsNoTracking()
            .Where(h => h.Status == HandoffStatus.PendingAcceptance);

        if (destinationArea.HasValue)
        {
            query = query.Where(h => h.DestinationArea == destinationArea.Value);
        }

        var list = await query
            .OrderByDescending(h => h.HandedOverAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<List<ClinicalHandoffDto>> GetHandoffsByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.ClinicalHandoffs
            .AsNoTracking()
            .Where(h => h.PatientId == patientId)
            .OrderByDescending(h => h.HandedOverAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    private static ClinicalHandoffDto MapToDto(ClinicalHandoff h) =>
        new(
            h.Id,
            h.HandoffProtocolNumber,
            h.PatientId,
            h.InpatientStayId,
            h.EncounterId,
            h.SourceArea,
            h.SourceLocationDetails,
            h.DestinationArea,
            h.DestinationLocationDetails,
            h.HandingOverStaffId,
            h.ReceivingStaffId,
            h.Situation,
            h.Background,
            h.Assessment,
            h.Recommendation,
            h.CriticalAlerts,
            h.Status,
            h.StatusReason,
            h.HandedOverAtUtc,
            h.AcceptedAtUtc,
            h.CreatedAtUtc,
            h.UpdatedAtUtc,
            h.Version);

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
            TargetResourceType: "ClinicalHandoff",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: detailsJson);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
