namespace HospitalManagement.Modules.Notifications.Application;

public sealed record NotificationProviderMessage(
    string Channel,
    string Recipient,
    string Subject,
    string Body,
    string IdempotencyKey,
    string TemplateKey,
    string Locale,
    int AttemptCount = 1);

public sealed record NotificationProviderResult(
    bool Succeeded,
    int AttemptCount,
    string? ErrorCode);

public interface INotificationTransport
{
    Task CaptureAsync(
        NotificationProviderMessage message,
        CancellationToken cancellationToken = default);
}

public interface INotificationProviderPort
{
    Task<NotificationProviderResult> DeliverAsync(
        NotificationProviderMessage message,
        CancellationToken cancellationToken = default);
}
