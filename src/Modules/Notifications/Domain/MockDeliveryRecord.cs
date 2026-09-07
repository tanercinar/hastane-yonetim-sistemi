namespace HospitalManagement.Modules.Notifications.Domain;

public sealed class MockDeliveryRecord
{
    private MockDeliveryRecord()
    {
    }

    public Guid Id
    {
        get; private set;
    }

    public string Channel
    {
        get; private set;
    } = string.Empty;

    public string Recipient
    {
        get; private set;
    } = string.Empty;

    public string Subject
    {
        get; private set;
    } = string.Empty;

    public string Body
    {
        get; private set;
    } = string.Empty;

    public string IdempotencyKey
    {
        get; private set;
    } = string.Empty;

    public DateTime SentAtUtc
    {
        get; private set;
    }

    public string TemplateKey { get; private set; } = string.Empty;

    public string Locale { get; private set; } = "tr-TR";

    public string Provider { get; private set; } = "MOCK-LOCAL-CAPTURE";

    public int AttemptCount
    {
        get; private set;
    }

    public static MockDeliveryRecord Create(
        Guid id,
        string channel,
        string recipient,
        string subject,
        string body,
        string idempotencyKey,
        DateTime nowUtc,
        string templateKey = "Notification.Generic",
        string locale = "tr-TR",
        string provider = "MOCK-LOCAL-CAPTURE",
        int attemptCount = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        if (attemptCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attemptCount),
                attemptCount,
                "Deneme sayısı en az bir olmalıdır.");
        }

        return new MockDeliveryRecord
        {
            Id = id,
            Channel = channel.Trim(),
            Recipient = recipient.Trim(),
            Subject = subject.Trim(),
            Body = body.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            SentAtUtc = nowUtc,
            TemplateKey = templateKey.Trim(),
            Locale = locale.Trim(),
            Provider = provider.Trim(),
            AttemptCount = attemptCount,
        };
    }
}
