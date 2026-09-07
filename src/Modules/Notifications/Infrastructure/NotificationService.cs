using System.Security.Claims;
using System.Text.Json;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Notifications.Application;
using HospitalManagement.Modules.Notifications.Domain;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Notifications.Infrastructure;

public sealed class NotificationService : INotificationService
{
    private readonly NotificationsDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly INotificationProviderPort _providerPort;
    private readonly TimeProvider _timeProvider;

    public NotificationService(
        NotificationsDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        INotificationProviderPort providerPort,
        TimeProvider? timeProvider = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _providerPort = providerPort ?? throw new ArgumentNullException(nameof(providerPort));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<bool> EnqueueOutboxEventAsync(
        PublishOutboxEventCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await _dbContext.OutboxEvents
            .AnyAsync(e => e.IdempotencyKey == command.IdempotencyKey, cancellationToken);

        if (existing)
        {
            return true;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var templateKey = string.IsNullOrWhiteSpace(command.TemplateKey)
            ? command.EventType
            : command.TemplateKey;
        var approvedTokens = NotificationTemplateCatalog.FilterApprovedTokens(
            templateKey,
            command.TemplateTokens);
        var outboxEvent = NotificationOutboxEvent.Create(
            Guid.NewGuid(),
            command.EventType,
            command.IdempotencyKey,
            command.RecipientPersonId,
            command.RecipientEmail,
            command.RecipientPhone,
            command.Subject,
            command.Message,
            now,
            templateKey,
            JsonSerializer.Serialize(approvedTokens));

        _dbContext.OutboxEvents.Add(outboxEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ProcessOutboxResultDto> ProcessOutboxAsync(
        CancellationToken cancellationToken = default)
    {
        var pendingEvents = await _dbContext.OutboxEvents
            .Where(e => e.Status == "Pending")
            .OrderBy(e => e.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        int processed = 0;
        int failed = 0;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var evt in pendingEvents)
        {
            try
            {
                // 1. In-App Notification (Idempotent)
                var existingInApp = await _dbContext.InAppNotifications
                    .AnyAsync(n => n.IdempotencyKey == evt.IdempotencyKey, cancellationToken);

                if (!existingInApp)
                {
                    var inApp = InAppNotification.Create(
                        Guid.NewGuid(),
                        evt.RecipientPersonId,
                        evt.Subject,
                        evt.Message,
                        actionUrl: "/patient/appointments",
                        evt.IdempotencyKey,
                        now);

                    _dbContext.InAppNotifications.Add(inApp);
                }

                var preference = await _dbContext.NotificationPreferences
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item => item.RecipientPersonId == evt.RecipientPersonId,
                        cancellationToken);
                var locale = preference?.Locale ?? "tr-TR";
                var emailEnabled = preference?.EmailEnabled ?? true;
                var smsEnabled = preference?.SmsEnabled ?? false;
                var tokens = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    evt.TemplateTokensJson)
                    ?? new Dictionary<string, string>(StringComparer.Ordinal);
                var rendered = NotificationTemplateCatalog.RenderExternal(
                    evt.TemplateKey,
                    locale,
                    tokens);

                if (emailEnabled && !string.IsNullOrWhiteSpace(evt.RecipientEmail))
                {
                    var result = await _providerPort.DeliverAsync(
                        new NotificationProviderMessage(
                            "Email",
                            evt.RecipientEmail,
                            rendered.Subject,
                            rendered.Body,
                            evt.IdempotencyKey,
                            rendered.TemplateKey,
                            rendered.Locale),
                        cancellationToken);
                    evt.RecordDeliveryAttempts(result.AttemptCount);
                    if (!result.Succeeded)
                    {
                        throw new NotificationProviderException(result.ErrorCode);
                    }
                }

                if (smsEnabled && !string.IsNullOrWhiteSpace(evt.RecipientPhone))
                {
                    var result = await _providerPort.DeliverAsync(
                        new NotificationProviderMessage(
                            "Sms",
                            evt.RecipientPhone,
                            rendered.Subject,
                            rendered.Body,
                            evt.IdempotencyKey,
                            rendered.TemplateKey,
                            rendered.Locale),
                        cancellationToken);
                    evt.RecordDeliveryAttempts(result.AttemptCount);
                    if (!result.Succeeded)
                    {
                        throw new NotificationProviderException(result.ErrorCode);
                    }
                }

                evt.MarkProcessed(now);
                await _dbContext.SaveChangesAsync(cancellationToken);
                processed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                evt.MarkFailed($"DeliveryFailed:{ex.GetType().Name}");
                await _dbContext.SaveChangesAsync(cancellationToken);
                failed++;
            }
        }

        return new ProcessOutboxResultDto(processed, failed);
    }

    private sealed class NotificationProviderException(string? errorCode)
        : Exception(errorCode ?? "MOCK_NOTIFICATION_PROVIDER_FAILED");

    public async Task<IReadOnlyList<InAppNotificationDto>> GetMyNotificationsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var personIdStr = user.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        if (!Guid.TryParse(personIdStr, out var personId))
        {
            return [];
        }

        var notifications = await _dbContext.InAppNotifications
            .AsNoTracking()
            .Where(n => n.RecipientPersonId == personId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return notifications.Select(n => new InAppNotificationDto(
            n.Id,
            n.RecipientPersonId,
            n.Title,
            n.Message,
            n.ActionUrl,
            n.IsRead,
            n.CreatedAtUtc,
            n.ReadAtUtc)).ToList();
    }

    public async Task<bool> MarkAsReadAsync(
        ClaimsPrincipal user,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var personIdStr = user.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        if (!Guid.TryParse(personIdStr, out var personId))
        {
            return false;
        }

        var notification = await _dbContext.InAppNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientPersonId == personId, cancellationToken);

        if (notification is null)
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        notification.MarkAsRead(now);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            user,
            "Notification.MarkRead",
            notification.Id.ToString(),
            AuditOutcome.Success,
            $"Bildirim okundu olarak işaretlendi: NotificationId={notification.Id}",
            cancellationToken);

        return true;
    }

    public async Task<NotificationPreferenceDto?> GetMyPreferenceAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!TryGetActorPersonId(user, out var personId))
        {
            return null;
        }

        var preference = await _dbContext.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.RecipientPersonId == personId,
                cancellationToken);

        return preference is null
            ? new NotificationPreferenceDto(
                EmailEnabled: true,
                SmsEnabled: false,
                Locale: "tr-TR",
                UpdatedAtUtc: null)
            : MapPreference(preference);
    }

    public async Task<NotificationPreferenceDto?> UpdateMyPreferenceAsync(
        ClaimsPrincipal user,
        UpdateNotificationPreferenceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(command);

        if (!TryGetActorPersonId(user, out var personId))
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var preference = await _dbContext.NotificationPreferences
            .FirstOrDefaultAsync(
                item => item.RecipientPersonId == personId,
                cancellationToken);

        if (preference is null)
        {
            preference = NotificationPreference.Create(
                personId,
                command.EmailEnabled,
                command.SmsEnabled,
                command.Locale,
                now);
            _dbContext.NotificationPreferences.Add(preference);
        }
        else
        {
            preference.Update(
                command.EmailEnabled,
                command.SmsEnabled,
                command.Locale,
                now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await PublishAuditAsync(
            user,
            "Notification.PreferenceUpdate",
            personId.ToString(),
            AuditOutcome.Success,
            cancellationToken: cancellationToken);

        return MapPreference(preference);
    }

    private static NotificationPreferenceDto MapPreference(NotificationPreference preference) =>
        new(
            preference.EmailEnabled,
            preference.SmsEnabled,
            preference.Locale,
            preference.UpdatedAtUtc);

    private static bool TryGetActorPersonId(ClaimsPrincipal actor, out Guid personId)
    {
        var personIdText = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        return Guid.TryParse(personIdText, out personId);
    }

    private Task PublishAuditAsync(
        ClaimsPrincipal actor,
        string action,
        string targetResourceId,
        AuditOutcome outcome,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        Guid? actorUserId = null;
        Guid? actorPersonId = null;

        var userIdStr = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdStr, out var uid))
        {
            actorUserId = uid;
        }

        var personIdStr = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        if (Guid.TryParse(personIdStr, out var pid))
        {
            actorPersonId = pid;
        }

        var actorRole = actor.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: _timeProvider.GetUtcNow().UtcDateTime,
            ActorUserId: actorUserId,
            ActorPersonId: actorPersonId,
            ActorRole: actorRole,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: "Notifications",
            TargetResourceId: targetResourceId,
            Outcome: outcome,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        return _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
