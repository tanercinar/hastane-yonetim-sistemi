namespace HospitalManagement.Contracts.Notifications;

public sealed record NotificationDetailResponse(
    Guid Id,
    string Title,
    string Message,
    string? ActionUrl,
    bool IsRead,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);

public sealed record ProcessOutboxResponse(
    int ProcessedCount,
    int FailedCount);

public sealed record NotificationPreferenceResponse(
    bool EmailEnabled,
    bool SmsEnabled,
    string Locale,
    DateTime? UpdatedAtUtc);
