namespace HospitalManagement.Modules.Notifications.Domain;

public sealed class NotificationOutboxEvent
{
    private NotificationOutboxEvent()
    {
    }

    public Guid Id
    {
        get; private set;
    }

    public string EventType
    {
        get; private set;
    } = string.Empty;

    public string IdempotencyKey
    {
        get; private set;
    } = string.Empty;

    public Guid RecipientPersonId
    {
        get; private set;
    }

    public string? RecipientEmail
    {
        get; private set;
    }

    public string? RecipientPhone
    {
        get; private set;
    }

    public string Subject
    {
        get; private set;
    } = string.Empty;

    public string Message
    {
        get; private set;
    } = string.Empty;

    public string TemplateKey
    {
        get; private set;
    } = string.Empty;

    public string TemplateTokensJson
    {
        get; private set;
    } = "{}";

    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public DateTime? ProcessedAtUtc
    {
        get; private set;
    }

    public int RetryCount
    {
        get; private set;
    }

    public string Status
    {
        get; private set;
    } = "Pending";

    public string? Error
    {
        get; private set;
    }

    public static NotificationOutboxEvent Create(
        Guid id,
        string eventType,
        string idempotencyKey,
        Guid recipientPersonId,
        string? recipientEmail,
        string? recipientPhone,
        string subject,
        string message,
        DateTime nowUtc,
        string? templateKey = null,
        string templateTokensJson = "{}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new NotificationOutboxEvent
        {
            Id = id,
            EventType = eventType.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            RecipientPersonId = recipientPersonId,
            RecipientEmail = string.IsNullOrWhiteSpace(recipientEmail) ? null : recipientEmail.Trim(),
            RecipientPhone = string.IsNullOrWhiteSpace(recipientPhone) ? null : recipientPhone.Trim(),
            Subject = subject.Trim(),
            Message = message.Trim(),
            TemplateKey = string.IsNullOrWhiteSpace(templateKey) ? eventType.Trim() : templateKey.Trim(),
            TemplateTokensJson = string.IsNullOrWhiteSpace(templateTokensJson) ? "{}" : templateTokensJson,
            CreatedAtUtc = nowUtc,
            Status = "Pending",
            RetryCount = 0,
        };
    }

    public void MarkProcessed(DateTime nowUtc)
    {
        Status = "Processed";
        ProcessedAtUtc = nowUtc;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        Status = "Failed";
        RetryCount++;
        Error = error;
    }

    public void RecordDeliveryAttempts(int attemptCount)
    {
        RetryCount += Math.Max(0, attemptCount - 1);
    }
}
