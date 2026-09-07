using System.Text.RegularExpressions;

namespace HospitalManagement.UI.Notifications;

/// <summary>
/// Enforces healthcare privacy by filtering Protected Health Information (PHI)
/// from native notification previews and lock screen banners.
/// </summary>
public static partial class SafeNativeNotificationFormatter
{
    private static readonly string[] SensitiveMedicalKeywords =
    [
        "kanser", "hiv", "aids", "diyabet", "hipertansiyon", "biyopsi", "tümör",
        "parol", "arveles", "insülin", "antibiyotik", "kemoterapi", "ameliyat",
        "pozitif", "negatif", "patoloji", "malign", "benign", "gebelik", "kürtaj",
        "mg", "doz", "reçete no", "tani", "teşhis"
    ];

    [GeneratedRegex(@"\d+(\.\d+)?\s*(mg|ml|iu|g/dl|mmol|mmhg)", RegexOptions.IgnoreCase)]
    private static partial Regex ClinicalUnitsRegex();

    public static SafeNotificationMessage CreateSafeNotification(
        SafeNotificationCategory category,
        string internalClinicalMessage,
        string? deepLinkUrl = null)
    {
        var (safeTitle, safePreview) = GetSafeDefaults(category);

        return new SafeNotificationMessage
        {
            Category = category,
            PublicLockScreenTitle = safeTitle,
            PublicLockScreenPreview = SanitizeForLockScreen(safePreview, internalClinicalMessage),
            AuthenticatedDetail = internalClinicalMessage,
            DeepLinkUrl = deepLinkUrl ?? GetDefaultDeepLink(category)
        };
    }

    public static string SanitizeForLockScreen(string defaultSafePreview, string? potentiallySensitiveText)
    {
        if (string.IsNullOrWhiteSpace(potentiallySensitiveText))
        {
            return defaultSafePreview;
        }

        var lower = potentiallySensitiveText.ToLowerInvariant();
        foreach (var keyword in SensitiveMedicalKeywords)
        {
            if (lower.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                // Sensitive medical term detected; enforce safe generic preview
                return defaultSafePreview;
            }
        }

        // Also check for numeric lab results like "14.2 g/dL", "120/80", "+", "-"
        if (ClinicalUnitsRegex().IsMatch(potentiallySensitiveText))
        {
            return defaultSafePreview;
        }

        return defaultSafePreview;
    }

    private static (string Title, string Preview) GetSafeDefaults(SafeNotificationCategory category) => category switch
    {
        SafeNotificationCategory.Appointment => (
            "Randevu Güncellemesi",
            "Poliklinik randevunuzla ilgili bir güncelleme bulunmaktadır. Ayrıntılar için uygulamaya giriş yapınız."
        ),
        SafeNotificationCategory.Prescription => (
            "Yeni E-Reçete Düzenlendi",
            "Adınıza yeni bir reçete kaydı oluşturuldu. İlaç detayları için uygulamayı açınız."
        ),
        SafeNotificationCategory.DiagnosticResult => (
            "Sonuç Bildirimi",
            "Tanısal tetkik sonucunuz onaylandı. Raporunuzu görüntülemek için uygulamaya giriş yapınız."
        ),
        SafeNotificationCategory.StaffUrgent => (
            "Klinik Görev Uyarısı",
            "İş istasyonunuzda yeni bir klinik görev bulunmaktadır."
        ),
        _ => (
            "Hastane Bilgilendirmesi",
            "Sistemde yeni bir bildiriminiz bulunmaktadır. Ayrıntılar için uygulamaya giriş yapınız."
        )
    };

    private static string GetDefaultDeepLink(SafeNotificationCategory category) => category switch
    {
        SafeNotificationCategory.Appointment => "hospitalapp://appointments",
        SafeNotificationCategory.Prescription => "hospitalapp://prescriptions",
        SafeNotificationCategory.DiagnosticResult => "hospitalapp://results",
        SafeNotificationCategory.StaffUrgent => "hospitalapp://staff-workspace",
        _ => "hospitalapp://home"
    };
}
