namespace HospitalManagement.Modules.Notifications.Domain;

public sealed record RenderedNotificationTemplate(
    string TemplateKey,
    string Locale,
    string Subject,
    string Body);

public static class NotificationTemplateCatalog
{
    public static RenderedNotificationTemplate RenderExternal(
        string templateKey,
        string locale,
        IReadOnlyDictionary<string, string> tokens)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        ArgumentNullException.ThrowIfNull(tokens);

        var normalizedLocale = string.Equals(locale, "en-US", StringComparison.OrdinalIgnoreCase)
            ? "en-US"
            : "tr-TR";

        var approvedTokens = FilterApprovedTokens(templateKey, tokens);

        return templateKey switch
        {
            "Appointment.Booked" => RenderBooked(normalizedLocale, approvedTokens),
            "Appointment.Cancelled" => RenderCancelled(normalizedLocale, approvedTokens),
            "Appointment.CheckedIn" => RenderCheckedIn(normalizedLocale, approvedTokens),
            _ => RenderGeneric(normalizedLocale),
        };
    }

    public static IReadOnlyDictionary<string, string> FilterApprovedTokens(
        string templateKey,
        IReadOnlyDictionary<string, string>? tokens)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);

        var approvedNames = templateKey switch
        {
            "Appointment.Booked" => new[] { "appointmentDate", "reference" },
            "Appointment.Cancelled" => new[] { "appointmentDate" },
            "Appointment.CheckedIn" => new[] { "queueNumber" },
            _ => [],
        };

        if (tokens is null || approvedNames.Length == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return approvedNames
            .Where(tokens.ContainsKey)
            .ToDictionary(
                name => name,
                name => tokens[name].Trim(),
                StringComparer.Ordinal);
    }

    private static RenderedNotificationTemplate RenderBooked(
        string locale,
        IReadOnlyDictionary<string, string> tokens)
    {
        var appointmentDate = GetApprovedToken(tokens, "appointmentDate");
        var reference = GetApprovedToken(tokens, "reference");
        return locale == "en-US"
            ? new RenderedNotificationTemplate(
                "Appointment.Booked",
                locale,
                "Your appointment is confirmed",
                $"Your appointment for {appointmentDate} is confirmed. Reference: {reference}.")
            : new RenderedNotificationTemplate(
                "Appointment.Booked",
                locale,
                "Randevunuz Onaylandı",
                $"{appointmentDate} tarihindeki randevunuz onaylandı. Referans: {reference}.");
    }

    private static RenderedNotificationTemplate RenderCancelled(
        string locale,
        IReadOnlyDictionary<string, string> tokens)
    {
        var appointmentDate = GetApprovedToken(tokens, "appointmentDate");
        return locale == "en-US"
            ? new RenderedNotificationTemplate(
                "Appointment.Cancelled",
                locale,
                "Your appointment was cancelled",
                $"Your appointment for {appointmentDate} was cancelled. Sign in to the secure portal for details.")
            : new RenderedNotificationTemplate(
                "Appointment.Cancelled",
                locale,
                "Randevunuz İptal Edildi",
                $"{appointmentDate} tarihindeki randevunuz iptal edildi. Ayrıntılar için güvenli portala giriş yapın.");
    }

    private static RenderedNotificationTemplate RenderCheckedIn(
        string locale,
        IReadOnlyDictionary<string, string> tokens)
    {
        var queueNumber = GetApprovedToken(tokens, "queueNumber");
        return locale == "en-US"
            ? new RenderedNotificationTemplate(
                "Appointment.CheckedIn",
                locale,
                "Your check-in is complete",
                $"Your check-in is complete. Queue number: {queueNumber}.")
            : new RenderedNotificationTemplate(
                "Appointment.CheckedIn",
                locale,
                "Randevu Girişiniz Tamamlandı",
                $"Girişiniz tamamlandı. Sıra numaranız: {queueNumber}.");
    }

    private static RenderedNotificationTemplate RenderGeneric(string locale) =>
        locale == "en-US"
            ? new RenderedNotificationTemplate(
                "Notification.Generic",
                locale,
                "New hospital portal notification",
                "You have a new notification. Sign in to the secure portal for details.")
            : new RenderedNotificationTemplate(
                "Notification.Generic",
                locale,
                "Yeni hastane portalı bildirimi",
                "Yeni bir bildiriminiz var. Ayrıntılar için güvenli portala giriş yapın.");

    private static string GetApprovedToken(IReadOnlyDictionary<string, string> tokens, string name)
    {
        if (!tokens.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Şablon için zorunlu '{name}' değeri eksik.", nameof(tokens));
        }

        return value.Trim();
    }
}
