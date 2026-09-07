using HospitalManagement.UI.Notifications;
using Xunit;

namespace HospitalManagement.ComponentTests.Security;

public sealed class NativeDeepLinkAndNotificationSecurityTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F12-G06")]
    public void SafeNativeNotificationFormatterStripsProtectedHealthInformationFromLockScreenPreview()
    {
        var sensitiveMessage = "Hastaya Tip 2 Diyabet ve Hipertansiyon teşhisi konuldu. Arveles 25mg ve Parol 500mg yazıldı.";

        var notification = SafeNativeNotificationFormatter.CreateSafeNotification(
            SafeNotificationCategory.Prescription,
            sensitiveMessage,
            "hospitalapp://prescriptions?id=DEMO-RX-01");

        // Public lock screen must NOT contain sensitive clinical terms
        Assert.DoesNotContain("Diyabet", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Hipertansiyon", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Arveles", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Parol", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("25mg", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);

        // Safe preview is generic and safe for public view
        Assert.Equal("Yeni E-Reçete Düzenlendi", notification.PublicLockScreenTitle);
        Assert.Contains("Adınıza yeni bir reçete kaydı oluşturuldu", notification.PublicLockScreenPreview, StringComparison.Ordinal);

        // Full clinical details remain inside AuthenticatedDetail for viewing after app unlock
        Assert.Equal(sensitiveMessage, notification.AuthenticatedDetail);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F12-G06")]
    public void SafeNativeNotificationFormatterStripsLaboratoryResultsFromPreview()
    {
        var labMessage = "HbA1c sonucu 8.4 g/dL, açlık kan şekeri 180 mg/dL olarak ölçüldü.";

        var notification = SafeNativeNotificationFormatter.CreateSafeNotification(
            SafeNotificationCategory.DiagnosticResult,
            labMessage);

        Assert.DoesNotContain("8.4", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("180", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("g/dL", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("kan şekeri", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);

        Assert.Equal("Sonuç Bildirimi", notification.PublicLockScreenTitle);
        Assert.Contains("Tanısal tetkik sonucunuz onaylandı", notification.PublicLockScreenPreview, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://evil.com/phishing")]
    [InlineData("https://hospital.demo/appointments")]
    [InlineData("file:///etc/passwd")]
    [InlineData("data:text/html,<script>window.location='https://evil.com'</script>")]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F12-G06")]
    public void NativeDeepLinkRouterRejectsNonAllowedSchemes(string maliciousUri)
    {
        var result = NativeDeepLinkRouter.Route(maliciousUri, userRole: "Patient", isAuthenticated: true);

        Assert.False(result.IsValid);
        Assert.Contains("Yetkisiz URI şeması", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("hospitalapp://appointments/../../admin/secret")]
    [InlineData("hospitalapp://appointments/..\\..\\admin")]
    [InlineData("hospitalapp://appointments//secret")]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F12-G06")]
    public void NativeDeepLinkRouterRejectsPathTraversalAttempts(string pathTraversalUri)
    {
        var result = NativeDeepLinkRouter.Route(pathTraversalUri, userRole: "Patient", isAuthenticated: true);

        Assert.False(result.IsValid);
        Assert.Contains("Geçersiz veya güvenlik kuralına aykırı", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("hospitalapp://appointments?id=<script>alert(1)</script>")]
    [InlineData("hospitalapp://appointments?id=';DROP TABLE patients;--")]
    [InlineData("hospitalapp://appointments?id=invalid space")]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F12-G06")]
    public void NativeDeepLinkRouterRejectsMaliciousQueryParameters(string maliciousQueryUri)
    {
        var result = NativeDeepLinkRouter.Route(maliciousQueryUri, userRole: "Patient", isAuthenticated: true);

        Assert.False(result.IsValid);
        Assert.Contains("Geçersiz kaynak kimlik formatı", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F12-G06")]
    public void NativeDeepLinkRouterRejectsPatientAccessToStaffWorkspace()
    {
        var result = NativeDeepLinkRouter.Route("hospitalapp://staff-workspace", userRole: "Patient", isAuthenticated: true);

        Assert.True(result.IsValid);
        Assert.False(result.IsAuthorized);
        Assert.Equal("/forbidden", result.TargetRoute);
        Assert.Contains("yetkiniz bulunmamaktadır", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F12-G06")]
    public void NativeDeepLinkRouterAllowsDoctorAccessToStaffWorkspace()
    {
        var result = NativeDeepLinkRouter.Route("hospitalapp://staff-workspace", userRole: "Doctor", isAuthenticated: true);

        Assert.True(result.IsValid);
        Assert.True(result.IsAuthorized);
        Assert.Equal("/staff/workspace", result.TargetRoute);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F12-G06")]
    public void NativeDeepLinkRouterParsesValidPatientRoutesSuccessfully()
    {
        var aptResult = NativeDeepLinkRouter.Route(
            "hospitalapp://appointments?id=DEMO-APT-20260904-001",
            userRole: "Patient",
            isAuthenticated: true);

        Assert.True(aptResult.IsValid);
        Assert.True(aptResult.IsAuthorized);
        Assert.Equal("/patient/appointments/DEMO-APT-20260904-001", aptResult.TargetRoute);
        Assert.Equal("DEMO-APT-20260904-001", aptResult.ResourceId);

        var rxResult = NativeDeepLinkRouter.Route(
            "hospitalapp://prescriptions?id=DEMO-RX-101",
            userRole: "Patient",
            isAuthenticated: true);

        Assert.True(rxResult.IsValid);
        Assert.Equal("/patient/prescriptions/DEMO-RX-101", rxResult.TargetRoute);

        var resResult = NativeDeepLinkRouter.Route(
            "hospitalapp://results",
            userRole: "Patient",
            isAuthenticated: true);

        Assert.True(resResult.IsValid);
        Assert.Equal("/patient/diagnostic-results", resResult.TargetRoute);
    }
}
