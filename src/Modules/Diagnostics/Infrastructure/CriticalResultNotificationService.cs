using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure;

public sealed class CriticalResultNotificationService : ICriticalResultNotificationService
{
    private readonly DiagnosticsDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly IDiagnosticsAccessContext _accessContext;
    private readonly IDiagnosticsRealtimeNotifier _realtimeNotifier;
    private readonly TimeProvider _timeProvider;

    public CriticalResultNotificationService(
        DiagnosticsDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        IDiagnosticsAccessContext accessContext,
        IDiagnosticsRealtimeNotifier realtimeNotifier,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _accessContext = accessContext ?? throw new ArgumentNullException(nameof(accessContext));
        _realtimeNotifier = realtimeNotifier ?? throw new ArgumentNullException(nameof(realtimeNotifier));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task ProcessAndNotifyCriticalResultsAsync(
        LabResult labResult,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(labResult);

        var criticalItems = labResult.Items
            .Where(i => i.Flag is LabResultInterpretation.CriticalLow or LabResultInterpretation.CriticalHigh)
            .ToList();

        if (criticalItems.Count == 0)
        {
            return;
        }

        var order = await _dbContext.DiagnosticOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == labResult.DiagnosticOrderId, cancellationToken);

        var responsibleDoctorId = order?.PlacingDoctorId;
        if (order is null || !responsibleDoctorId.HasValue)
        {
            return;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var createdNotifications = new List<CriticalResultNotification>();

        foreach (var item in criticalItems)
        {
            var exists = await _dbContext.CriticalResultNotifications
                .AnyAsync(n => n.LabResultId == labResult.Id && n.ParameterCode == item.ParameterCode, cancellationToken);

            if (!exists)
            {
                var notification = CriticalResultNotification.Create(
                    Guid.NewGuid(),
                    labResult.Id,
                    labResult.DiagnosticOrderId,
                    labResult.DiagnosticOrderItemId,
                    labResult.PatientId,
                    item.ParameterCode,
                    item.ParameterName,
                    item.NumericValue,
                    item.StringValue,
                    item.Unit,
                    item.Flag,
                    responsibleDoctorId,
                    nowUtc);

                _dbContext.CriticalResultNotifications.Add(notification);
                createdNotifications.Add(notification);
            }
        }

        if (createdNotifications.Count == 0)
        {
            return;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // A parallel finalization may have created the same notification first.
            // The database uniqueness constraint is the authoritative idempotency guard.
            _dbContext.ChangeTracker.Clear();
            return;
        }

        foreach (var notification in createdNotifications)
        {
            await PublishAuditAsync(
                responsibleDoctorId,
                null,
                "System",
                "Diagnostics.CriticalResultNotify",
                notification.Id,
                nowUtc,
                cancellationToken);
            await _realtimeNotifier.NotifyCriticalResultChangedAsync(
                notification.Id,
                responsibleDoctorId.Value,
                notification.Status.ToString(),
                cancellationToken);
        }
    }

    public async Task<DiagnosticOrderOperationResult<CriticalResultNotificationDto>> AcknowledgeAsync(
        ClaimsPrincipal actor,
        Guid notificationId,
        string acknowledgmentNotes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(acknowledgmentNotes))
        {
            return DiagnosticOrderOperationResult.Validation<CriticalResultNotificationDto>(
                "AcknowledgmentNotes",
                "Kritik değer teslim teyidi ve alındı notu zorunludur.");
        }

        var notification = await _dbContext.CriticalResultNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification is null)
        {
            return DiagnosticOrderOperationResult.NotFound<CriticalResultNotificationDto>("Kritik sonuç bildirimi bulunamadı.");
        }

        if (!await CanAccessNotificationAsync(actor, notification, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<CriticalResultNotificationDto>(
                "Bu kritik bildirim için bakım ilişkisi veya bölüm kapsamı bulunmamaktadır.");
        }

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            notification.Acknowledge(actorUserId, acknowledgmentNotes, nowUtc);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<CriticalResultNotificationDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<CriticalResultNotificationDto>("Bildirim kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actorUserId,
            GetActorPersonId(actor),
            actor.FindFirst(ClaimTypes.Role)?.Value,
            "Diagnostics.CriticalResultAcknowledge",
            notification.Id,
            nowUtc,
            cancellationToken);
        if (notification.ResponsibleDoctorUserId.HasValue)
        {
            await _realtimeNotifier.NotifyCriticalResultChangedAsync(
                notification.Id,
                notification.ResponsibleDoctorUserId.Value,
                notification.Status.ToString(),
                cancellationToken);
        }

        return DiagnosticOrderOperationResult.Success(MapToDto(notification));
    }

    public async Task<DiagnosticOrderOperationResult<CriticalResultNotificationDto>> EscalateAsync(
        ClaimsPrincipal actor,
        Guid notificationId,
        string escalationReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(escalationReason))
        {
            return DiagnosticOrderOperationResult.Validation<CriticalResultNotificationDto>(
                "EscalationReason",
                "Eskalasyon gerekçesi zorunludur.");
        }

        var notification = await _dbContext.CriticalResultNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification is null)
        {
            return DiagnosticOrderOperationResult.NotFound<CriticalResultNotificationDto>("Kritik sonuç bildirimi bulunamadı.");
        }

        if (!await CanAccessNotificationAsync(actor, notification, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<CriticalResultNotificationDto>(
                "Bu kritik bildirimi eskale etme kaynak yetkiniz bulunmamaktadır.");
        }

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            notification.Escalate(escalationReason, nowUtc);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<CriticalResultNotificationDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<CriticalResultNotificationDto>("Bildirim kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actorUserId,
            GetActorPersonId(actor),
            actor.FindFirst(ClaimTypes.Role)?.Value,
            "Diagnostics.CriticalResultEscalate",
            notification.Id,
            nowUtc,
            cancellationToken);
        if (notification.ResponsibleDoctorUserId.HasValue)
        {
            await _realtimeNotifier.NotifyCriticalResultChangedAsync(
                notification.Id,
                notification.ResponsibleDoctorUserId.Value,
                notification.Status.ToString(),
                cancellationToken);
        }

        return DiagnosticOrderOperationResult.Success(MapToDto(notification));
    }

    public async Task<DiagnosticOrderOperationResult<IReadOnlyList<CriticalResultNotificationDto>>> GetActiveNotificationsAsync(
        ClaimsPrincipal actor,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var query = _dbContext.CriticalResultNotifications
            .AsNoTracking()
            .Where(n => n.Status == CriticalNotificationStatus.Active || n.Status == CriticalNotificationStatus.Escalated);

        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            query = query.Where(n => n.PatientId == patientId.Value);
        }

        var list = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = new List<CriticalResultNotificationDto>();
        foreach (var notification in list)
        {
            if (await CanAccessNotificationAsync(actor, notification, cancellationToken))
            {
                dtos.Add(MapToDto(notification));
            }
        }
        return DiagnosticOrderOperationResult.Success<IReadOnlyList<CriticalResultNotificationDto>>(dtos);
    }

    public async Task<DiagnosticOrderOperationResult<CriticalResultNotificationDto>> GetByIdAsync(
        ClaimsPrincipal actor,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var notification = await _dbContext.CriticalResultNotifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification is null)
        {
            return DiagnosticOrderOperationResult.NotFound<CriticalResultNotificationDto>("Kritik sonuç bildirimi bulunamadı.");
        }

        if (!await CanAccessNotificationAsync(actor, notification, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<CriticalResultNotificationDto>(
                "Bu kritik bildirim için bakım ilişkisi veya bölüm kapsamı bulunmamaktadır.");
        }

        return DiagnosticOrderOperationResult.Success(MapToDto(notification));
    }

    private static CriticalResultNotificationDto MapToDto(CriticalResultNotification n) =>
        new(
            n.Id,
            n.LabResultId,
            n.DiagnosticOrderId,
            n.DiagnosticOrderItemId,
            n.PatientId,
            n.ParameterCode,
            n.ParameterName,
            n.NumericValue,
            n.StringValue,
            n.Unit,
            n.Flag,
            n.Status,
            n.EscalationLevel,
            n.ResponsibleDoctorUserId,
            n.AcknowledgedByUserId,
            n.AcknowledgedAtUtc,
            n.AcknowledgmentNotes,
            n.EscalatedAtUtc,
            n.EscalationReason,
            n.CreatedAtUtc);

    private static Guid GetActorUserId(ClaimsPrincipal actor)
    {
        var claim = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var uid) ? uid : Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private static Guid? GetActorPersonId(ClaimsPrincipal actor)
    {
        var claim = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        return Guid.TryParse(claim, out var personId) ? personId : null;
    }

    private Task<bool> CanAccessNotificationAsync(
        ClaimsPrincipal actor,
        CriticalResultNotification notification,
        CancellationToken cancellationToken) =>
        _accessContext.CanAccessPatientAsync(
            actor,
            notification.PatientId,
            HospitalPermissions.ClinicalRecords.EncounterView,
            false,
            cancellationToken);

    private async Task PublishAuditAsync(
        Guid? actorUserId,
        Guid? actorPersonId,
        string? actorRole,
        string action,
        Guid notificationId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await _auditPublisher.PublishAsync(
            new AuditEvent(
                Guid.NewGuid(),
                nowUtc,
                actorUserId,
                actorPersonId,
                actorRole,
                null,
                null,
                action,
                "CriticalResultNotification",
                notificationId.ToString(),
                AuditOutcome.Success,
                null,
                Guid.NewGuid().ToString("D"),
                null),
            cancellationToken);
    }
}
