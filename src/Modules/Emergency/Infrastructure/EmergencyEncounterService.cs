using System.Text.Json;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.Emergency.Application;
using HospitalManagement.Modules.Emergency.Domain;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Emergency.Infrastructure;

public sealed class EmergencyEncounterService : IEmergencyEncounterService
{
    private static readonly EmergencyAdmissionStatus[] ModifiableStatuses =
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

    public EmergencyEncounterService(
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

    public async Task<EmergencyOperationResult<EmergencyOrderDto>> CreateOrderAsync(
        CreateEmergencyOrderDto dto,
        Guid doctorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (doctorId == Guid.Empty)
        {
            return EmergencyOperationResult.Validation<EmergencyOrderDto>("OrderedByDoctorId", "İstemi yapan hekim seçilmelidir.");
        }

        if (string.IsNullOrWhiteSpace(dto.OrderCatalogCode))
        {
            return EmergencyOperationResult.Validation<EmergencyOrderDto>("OrderCatalogCode", "İstem katalog kodu boş olamaz.");
        }

        if (string.IsNullOrWhiteSpace(dto.OrderCatalogName))
        {
            return EmergencyOperationResult.Validation<EmergencyOrderDto>("OrderCatalogName", "İstem katalog adı boş olamaz.");
        }

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == dto.AdmissionId, cancellationToken);
        if (admission is null)
        {
            return EmergencyOperationResult.NotFound<EmergencyOrderDto>("Acil başvurusu bulunamadı.");
        }

        if (!ModifiableStatuses.Contains(admission.Status))
        {
            return EmergencyOperationResult.Conflict<EmergencyOrderDto>(
                $"Sonuçlandırılmış başvuruya ({admission.Status}) yeni istem eklenemez.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var order = EmergencyCareOrder.Create(
            Guid.NewGuid(),
            dto.AdmissionId,
            dto.OrderType,
            dto.OrderCatalogCode,
            dto.OrderCatalogName,
            dto.Priority,
            doctorId,
            dto.ClinicalInstructions,
            now);

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Emergency.OrderCreate",
            order.Id.ToString(),
            "Acil istemi oluşturuldu",
            doctorId,
            JsonSerializer.Serialize(new
            {
                OrderId = order.Id,
                AdmissionId = order.AdmissionId,
                OrderType = order.OrderType.ToString(),
                Priority = order.Priority.ToString(),
            }),
            now,
            cancellationToken);

        await _notifier.NotifyDashboardUpdatedAsync(cancellationToken);

        return EmergencyOperationResult.Success(MapToOrderDto(order));
    }

    public async Task<EmergencyOperationResult<EmergencyOrderDto>> CompleteOrderAsync(
        Guid orderId,
        string? resultSummary,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            return EmergencyOperationResult.NotFound<EmergencyOrderDto>("İstem bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            order.Complete(resultSummary, now);
        }
        catch (Exception ex)
        {
            return EmergencyOperationResult.Conflict<EmergencyOrderDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Emergency.OrderComplete",
            order.Id.ToString(),
            "Acil istemi tamamlandı/sonuçlandırıldı",
            staffId,
            JsonSerializer.Serialize(new
            {
                OrderId = order.Id,
            }),
            now,
            cancellationToken);

        await _notifier.NotifyDashboardUpdatedAsync(cancellationToken);

        return EmergencyOperationResult.Success(MapToOrderDto(order));
    }

    public async Task<EmergencyOperationResult<EmergencyOrderDto>> CancelOrderAsync(
        Guid orderId,
        string reason,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return EmergencyOperationResult.Validation<EmergencyOrderDto>("Reason", "İptal gerekçesi boş bırakılamaz.");
        }

        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            return EmergencyOperationResult.NotFound<EmergencyOrderDto>("İstem bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            order.Cancel(reason, now);
        }
        catch (Exception ex)
        {
            return EmergencyOperationResult.Conflict<EmergencyOrderDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Emergency.OrderCancel",
            order.Id.ToString(),
            "Acil istemi iptal edildi",
            staffId,
            JsonSerializer.Serialize(new
            {
                OrderId = order.Id,
            }),
            now,
            cancellationToken);

        await _notifier.NotifyDashboardUpdatedAsync(cancellationToken);

        return EmergencyOperationResult.Success(MapToOrderDto(order));
    }

    public async Task<List<EmergencyOrderDto>> GetOrdersByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.AdmissionId == admissionId)
            .OrderByDescending(o => o.OrderedAtUtc)
            .ToListAsync(cancellationToken);

        return orders.Select(MapToOrderDto).ToList();
    }

    public async Task<EmergencyOperationResult<EmergencyConsultationDto>> RequestConsultationAsync(
        RequestEmergencyConsultationDto dto,
        Guid doctorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (doctorId == Guid.Empty)
        {
            return EmergencyOperationResult.Validation<EmergencyConsultationDto>("RequestedByDoctorId", "Konsültasyon isteyen hekim seçilmelidir.");
        }

        if (dto.DepartmentId == Guid.Empty || string.IsNullOrWhiteSpace(dto.DepartmentName))
        {
            return EmergencyOperationResult.Validation<EmergencyConsultationDto>("Department", "Konsültasyon istenen bölüm belirtilmelidir.");
        }

        if (string.IsNullOrWhiteSpace(dto.ClinicalReason))
        {
            return EmergencyOperationResult.Validation<EmergencyConsultationDto>("ClinicalReason", "Konsültasyon klinik gerekçesi boş olamaz.");
        }

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == dto.AdmissionId, cancellationToken);
        if (admission is null)
        {
            return EmergencyOperationResult.NotFound<EmergencyConsultationDto>("Acil başvurusu bulunamadı.");
        }

        if (!ModifiableStatuses.Contains(admission.Status))
        {
            return EmergencyOperationResult.Conflict<EmergencyConsultationDto>(
                $"Sonuçlandırılmış başvuruya ({admission.Status}) yeni konsültasyon eklenemez.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var consultation = EmergencyConsultation.Create(
            Guid.NewGuid(),
            dto.AdmissionId,
            dto.DepartmentId,
            dto.DepartmentName,
            doctorId,
            dto.Urgency,
            dto.ClinicalReason,
            now);

        _dbContext.Consultations.Add(consultation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Emergency.ConsultationRequest",
            consultation.Id.ToString(),
            "Acil konsültasyon istendi",
            doctorId,
            JsonSerializer.Serialize(new
            {
                ConsultationId = consultation.Id,
                AdmissionId = consultation.AdmissionId,
                DepartmentId = consultation.DepartmentId,
                Urgency = consultation.Urgency.ToString(),
            }),
            now,
            cancellationToken);

        await _notifier.NotifyDashboardUpdatedAsync(cancellationToken);

        return EmergencyOperationResult.Success(MapToConsultationDto(consultation));
    }

    public async Task<EmergencyOperationResult<EmergencyConsultationDto>> AcceptConsultationAsync(
        Guid consultationId,
        Guid consultantDoctorId,
        CancellationToken cancellationToken = default)
    {
        if (consultantDoctorId == Guid.Empty)
        {
            return EmergencyOperationResult.Validation<EmergencyConsultationDto>("ConsultantDoctorId", "Konsültan hekim seçilmelidir.");
        }

        var consultation = await _dbContext.Consultations
            .FirstOrDefaultAsync(c => c.Id == consultationId, cancellationToken);
        if (consultation is null)
        {
            return EmergencyOperationResult.NotFound<EmergencyConsultationDto>("Konsültasyon bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            consultation.Accept(consultantDoctorId, now);
        }
        catch (Exception ex)
        {
            return EmergencyOperationResult.Conflict<EmergencyConsultationDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Emergency.ConsultationAccept",
            consultation.Id.ToString(),
            "Acil konsültasyon hekim tarafından kabul edildi",
            consultantDoctorId,
            JsonSerializer.Serialize(new
            {
                ConsultationId = consultation.Id,
                ConsultantDoctorId = consultantDoctorId,
            }),
            now,
            cancellationToken);

        await _notifier.NotifyDashboardUpdatedAsync(cancellationToken);

        return EmergencyOperationResult.Success(MapToConsultationDto(consultation));
    }

    public async Task<EmergencyOperationResult<EmergencyConsultationDto>> RespondConsultationAsync(
        Guid consultationId,
        string responseNotes,
        Guid consultantDoctorId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(responseNotes))
        {
            return EmergencyOperationResult.Validation<EmergencyConsultationDto>("ResponseNotes", "Konsültasyon yanıt notu boş olamaz.");
        }

        var consultation = await _dbContext.Consultations
            .FirstOrDefaultAsync(c => c.Id == consultationId, cancellationToken);
        if (consultation is null)
        {
            return EmergencyOperationResult.NotFound<EmergencyConsultationDto>("Konsültasyon bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            consultation.Respond(consultantDoctorId, responseNotes, now);
        }
        catch (Exception ex)
        {
            return EmergencyOperationResult.Conflict<EmergencyConsultationDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Emergency.ConsultationRespond",
            consultation.Id.ToString(),
            "Acil konsültasyonuna yanıt verildi",
            consultantDoctorId,
            JsonSerializer.Serialize(new
            {
                ConsultationId = consultation.Id,
            }),
            now,
            cancellationToken);

        await _notifier.NotifyDashboardUpdatedAsync(cancellationToken);

        return EmergencyOperationResult.Success(MapToConsultationDto(consultation));
    }

    public async Task<EmergencyOperationResult<EmergencyConsultationDto>> CancelConsultationAsync(
        Guid consultationId,
        string reason,
        Guid doctorId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return EmergencyOperationResult.Validation<EmergencyConsultationDto>("Reason", "İptal gerekçesi boş olamaz.");
        }

        var consultation = await _dbContext.Consultations
            .FirstOrDefaultAsync(c => c.Id == consultationId, cancellationToken);
        if (consultation is null)
        {
            return EmergencyOperationResult.NotFound<EmergencyConsultationDto>("Konsültasyon bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            consultation.Cancel(reason, now);
        }
        catch (Exception ex)
        {
            return EmergencyOperationResult.Conflict<EmergencyConsultationDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Emergency.ConsultationCancel",
            consultation.Id.ToString(),
            "Acil konsültasyonu iptal edildi",
            doctorId,
            JsonSerializer.Serialize(new
            {
                ConsultationId = consultation.Id,
            }),
            now,
            cancellationToken);

        await _notifier.NotifyDashboardUpdatedAsync(cancellationToken);

        return EmergencyOperationResult.Success(MapToConsultationDto(consultation));
    }

    public async Task<List<EmergencyConsultationDto>> GetConsultationsByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        var consultations = await _dbContext.Consultations
            .AsNoTracking()
            .Where(c => c.AdmissionId == admissionId)
            .OrderByDescending(c => c.RequestedAtUtc)
            .ToListAsync(cancellationToken);

        return consultations.Select(MapToConsultationDto).ToList();
    }

    public async Task<EmergencyOperationResult<EmergencyAdmissionDto>> RecordDispositionAsync(
        Guid admissionId,
        RecordEmergencyDispositionDto dto,
        Guid doctorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (doctorId == Guid.Empty)
        {
            return EmergencyOperationResult.Validation<EmergencyAdmissionDto>("DecidedByDoctorId", "Karar veren hekim seçilmelidir.");
        }

        if (string.IsNullOrWhiteSpace(dto.DispositionSummaryNotes))
        {
            return EmergencyOperationResult.Validation<EmergencyAdmissionDto>("DispositionSummaryNotes", "Disposition klinik karar özeti/epikriz notu boş olamaz.");
        }

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == admissionId, cancellationToken);
        if (admission is null)
        {
            return EmergencyOperationResult.NotFound<EmergencyAdmissionDto>("Acil başvurusu bulunamadı.");
        }

        if (!ModifiableStatuses.Contains(admission.Status))
        {
            return EmergencyOperationResult.Conflict<EmergencyAdmissionDto>(
                $"Sonuçlandırılmış başvuruya ({admission.Status}) yeni disposition kararı verilemez.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var oldStatus = admission.Status;
        try
        {
            admission.RecordDisposition(
                dto.DispositionType,
                doctorId,
                dto.TargetWardOrIcuId,
                dto.TargetDepartmentName,
                dto.DispositionSummaryNotes,
                dto.FollowUpInstructions,
                now);
        }
        catch (Exception ex)
        {
            return EmergencyOperationResult.Validation<EmergencyAdmissionDto>("Disposition", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Emergency.DispositionDecide",
            admission.Id.ToString(),
            $"Acil disposition kararı verildi: {dto.DispositionType} (Durum: {oldStatus} -> {admission.Status})",
            doctorId,
            JsonSerializer.Serialize(new
            {
                AdmissionId = admission.Id,
                DispositionType = dto.DispositionType.ToString(),
                NewStatus = admission.Status.ToString(),
            }),
            now,
            cancellationToken);

        await _notifier.NotifyStatusChangedAsync(
            admission.Id,
            admission.EmergencyProtocolNumber,
            oldStatus.ToString(),
            admission.Status.ToString(),
            cancellationToken);

        return EmergencyOperationResult.Success(MapToAdmissionDto(admission));
    }

    private static EmergencyOrderDto MapToOrderDto(EmergencyCareOrder o) =>
        new(
            o.Id,
            o.AdmissionId,
            o.OrderType,
            o.OrderCatalogCode,
            o.OrderCatalogName,
            o.Priority,
            o.OrderedByDoctorId,
            o.OrderedAtUtc,
            o.Status,
            o.ClinicalInstructions,
            o.ResultSummary,
            o.CompletedAtUtc,
            o.CancellationReason,
            o.Version);

    private static EmergencyConsultationDto MapToConsultationDto(EmergencyConsultation c) =>
        new(
            c.Id,
            c.AdmissionId,
            c.DepartmentId,
            c.DepartmentName,
            c.RequestedByDoctorId,
            c.RequestedAtUtc,
            c.Urgency,
            c.ClinicalReason,
            c.Status,
            c.ConsultantDoctorId,
            c.ConsultationResponseNotes,
            c.RespondedAtUtc,
            c.CancellationReason,
            c.Version);

    private static EmergencyAdmissionDto MapToAdmissionDto(EmergencyAdmission a) =>
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
            TargetResourceType: "EmergencyEncounter",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: detailsJson);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
