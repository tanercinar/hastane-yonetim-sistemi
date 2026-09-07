namespace HospitalManagement.Modules.Notifications.Domain;

public sealed class NotificationPreference
{
    private static readonly string[] SupportedLocales = ["tr-TR", "en-US"];

    private NotificationPreference()
    {
    }

    public Guid RecipientPersonId
    {
        get; private set;
    }

    public bool EmailEnabled
    {
        get; private set;
    }

    public bool SmsEnabled
    {
        get; private set;
    }

    public string Locale { get; private set; } = "tr-TR";

    public DateTime UpdatedAtUtc
    {
        get; private set;
    }

    public static NotificationPreference Create(
        Guid recipientPersonId,
        bool emailEnabled,
        bool smsEnabled,
        string locale,
        DateTime nowUtc)
    {
        if (recipientPersonId == Guid.Empty)
        {
            throw new ArgumentException("Alıcı kişi kimliği zorunludur.", nameof(recipientPersonId));
        }

        var preference = new NotificationPreference
        {
            RecipientPersonId = recipientPersonId,
        };
        preference.Update(emailEnabled, smsEnabled, locale, nowUtc);
        return preference;
    }

    public void Update(
        bool emailEnabled,
        bool smsEnabled,
        string locale,
        DateTime nowUtc)
    {
        var normalizedLocale = SupportedLocales.FirstOrDefault(
            item => string.Equals(item, locale, StringComparison.OrdinalIgnoreCase));

        if (normalizedLocale is null)
        {
            throw new ArgumentException("Desteklenen locale değerleri tr-TR ve en-US'tir.", nameof(locale));
        }

        EmailEnabled = emailEnabled;
        SmsEnabled = smsEnabled;
        Locale = normalizedLocale;
        UpdatedAtUtc = nowUtc;
    }
}
