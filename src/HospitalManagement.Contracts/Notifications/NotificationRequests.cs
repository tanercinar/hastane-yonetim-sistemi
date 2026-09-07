namespace HospitalManagement.Contracts.Notifications;

public sealed record PublishNotificationEventRequest(
    string EventType,
    string IdempotencyKey,
    Guid RecipientPersonId,
    string? RecipientEmail,
    string? RecipientPhone,
    string Subject,
    string Message);

public sealed record UpdateNotificationPreferenceRequest(
    bool EmailEnabled,
    bool SmsEnabled,
    string Locale);
