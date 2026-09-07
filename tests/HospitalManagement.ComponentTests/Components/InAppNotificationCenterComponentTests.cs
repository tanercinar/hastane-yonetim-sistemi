using System.Globalization;
using System.Net;
using HospitalManagement.UI.Components.Notifications;
using HospitalManagement.UI.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HospitalManagement.ComponentTests.Components;

public sealed class InAppNotificationCenterComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F12-G06")]
    public async Task InAppNotificationCenterRendersEmptyStateWhenNoNotifications()
    {
        var markup = await RenderAsync();
        var visibleText = WebUtility.HtmlDecode(markup);

        Assert.Contains("Bildirimler", visibleText, StringComparison.Ordinal);
        Assert.Contains("Bildiriminiz Yok", visibleText, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F12-G06")]
    public async Task InAppNotificationCenterRendersNotificationsAndUnreadBadge()
    {
        var notifications = new List<SafeNotificationMessage>
        {
            new()
            {
                Category = SafeNotificationCategory.Appointment,
                PublicLockScreenTitle = "Randevu Güncellemesi",
                PublicLockScreenPreview = "Randevunuzla ilgili güncelleme var.",
                AuthenticatedDetail = "Dr. Ahmet ile olan randevunuz onaylandı.",
                DeepLinkUrl = "hospitalapp://appointments?id=DEMO-APT-01",
                IsRead = false
            },
            new()
            {
                Category = SafeNotificationCategory.Prescription,
                PublicLockScreenTitle = "Yeni E-Reçete Düzenlendi",
                PublicLockScreenPreview = "Yeni reçeteniz hazır.",
                AuthenticatedDetail = "2 kutu ilaç reçetenize eklendi.",
                DeepLinkUrl = "hospitalapp://prescriptions?id=DEMO-RX-01",
                IsRead = true
            }
        };

        var markup = await RenderAsync(new Dictionary<string, object?>
        {
            [nameof(InAppNotificationCenter.Notifications)] = notifications
        });
        var visibleText = WebUtility.HtmlDecode(markup);

        Assert.Contains("Randevu Güncellemesi", visibleText, StringComparison.Ordinal);
        Assert.Contains("Dr. Ahmet ile olan randevunuz onaylandı", visibleText, StringComparison.Ordinal);
        Assert.Contains("Yeni E-Reçete Düzenlendi", visibleText, StringComparison.Ordinal);
        Assert.Contains("2 kutu ilaç reçetenize eklendi", visibleText, StringComparison.Ordinal);
        Assert.Contains("İlgili Kayda Git", visibleText, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync(IDictionary<string, object?>? parameters = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();

        using var serviceProvider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(
            serviceProvider,
            serviceProvider.GetRequiredService<ILoggerFactory>());

        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var turkishCulture = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.CurrentCulture = turkishCulture;
            CultureInfo.CurrentUICulture = turkishCulture;

            return await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameterView = parameters is null
                    ? ParameterView.Empty
                    : ParameterView.FromDictionary(parameters);
                var renderedComponent = await renderer.RenderComponentAsync<InAppNotificationCenter>(parameterView);
                return renderedComponent.ToHtmlString();
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
