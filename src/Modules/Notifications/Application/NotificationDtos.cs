namespace HospitalManagement.Modules.Notifications.Application;

public sealed record InAppNotificationDto(
    Guid Id,
    Guid RecipientPersonId,
    string Title,
    string Message,
    string? ActionUrl,
    bool IsRead,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);

public sealed record ProcessOutboxResultDto(
    int ProcessedCount,
    int FailedCount);

public sealed record PublishOutboxEventCommand(
    string EventType,
    string IdempotencyKey,
    Guid RecipientPersonId,
    string? RecipientEmail,
    string? RecipientPhone,
    string Subject,
    string Message,
    string? TemplateKey = null,
    IReadOnlyDictionary<string, string>? TemplateTokens = null);

public sealed record NotificationPreferenceDto(
    bool EmailEnabled,
    bool SmsEnabled,
    string Locale,
    DateTime? UpdatedAtUtc);

public sealed record UpdateNotificationPreferenceCommand(
    bool EmailEnabled,
    bool SmsEnabled,
    string Locale);

public sealed record LocalNotificationCaptureDto(
    Guid Id,
    string Channel,
    string Recipient,
    string Subject,
    string Body,
    string IdempotencyKey,
    string TemplateKey,
    string Locale,
    string Provider,
    int AttemptCount,
    DateTime SentAtUtc);
