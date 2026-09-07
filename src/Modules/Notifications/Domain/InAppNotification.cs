namespace HospitalManagement.Modules.Notifications.Domain;

public sealed class InAppNotification
{
    private InAppNotification()
    {
    }

    public Guid Id
    {
        get; private set;
    }

    public Guid RecipientPersonId
    {
        get; private set;
    }

    public string Title
    {
        get; private set;
    } = string.Empty;

    public string Message
    {
        get; private set;
    } = string.Empty;

    public string? ActionUrl
    {
        get; private set;
    }

    public bool IsRead
    {
        get; private set;
    }

    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public DateTime? ReadAtUtc
    {
        get; private set;
    }

    public string IdempotencyKey
    {
        get; private set;
    } = string.Empty;

    public static InAppNotification Create(
        Guid id,
        Guid recipientPersonId,
        string title,
        string message,
        string? actionUrl,
        string idempotencyKey,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        return new InAppNotification
        {
            Id = id,
            RecipientPersonId = recipientPersonId,
            Title = title.Trim(),
            Message = message.Trim(),
            ActionUrl = string.IsNullOrWhiteSpace(actionUrl) ? null : actionUrl.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            IsRead = false,
            CreatedAtUtc = nowUtc,
        };
    }

    public void MarkAsRead(DateTime nowUtc)
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAtUtc = nowUtc;
        }
    }
}
