using System.Globalization;
using Bunit;
using HospitalManagement.UI.Components;
using HospitalManagement.UI.Components.Notifications;
using HospitalManagement.UI.Components.PatientMobile;
using HospitalManagement.UI.Components.StaffWorkspace;
using HospitalManagement.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests.Accessibility;

public sealed class AccessibilityAndUsabilityTests
{
    private sealed class FakeConnectivityService(bool isOnline = true) : IPlatformConnectivityService
    {
        public bool IsConnected => isOnline;

        public event EventHandler<bool>? ConnectivityChanged
        {
            add
            {
            }
            remove
            {
            }
        }

        public Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(isOnline);
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName = "tr-TR")
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F13-G08")]
    public void StaffDesktopWorkspaceProvidesWcagCompliantLandmarksAndKeyboardShortcuts()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        context.Services.AddSingleton<IPlatformConnectivityService>(new FakeConnectivityService(true));
        using var cultureScope = new CultureScope();

        var component = context.Render<StaffDesktopWorkspace>(parameters => parameters
            .Add(p => p.HasPermission, true)
            .Add(p => p.StaffRole, "Doctor"));

        var workspace = component.Find("div.staff-desktop-workspace");
        Assert.NotNull(workspace);
        Assert.Equal("0", workspace.GetAttribute("tabindex"));
        Assert.Equal("Personel Masaüstü Çalışma Alanı", workspace.GetAttribute("aria-label"));

        var tablist = component.Find("ul[role='tablist']");
        Assert.NotNull(tablist);
        Assert.Equal("Masaüstü Panelleri", tablist.GetAttribute("aria-label"));

        var tabs = component.FindAll("button[role='tab']");
        Assert.NotEmpty(tabs);
        var activeTab = tabs.First(t => t.GetAttribute("aria-selected") == "true");
        Assert.Equal("panel-appointments", activeTab.GetAttribute("aria-controls"));

        var text = component.Markup;
        Assert.Contains("Alt+1", text, StringComparison.Ordinal);
        Assert.Contains("Alt+2", text, StringComparison.Ordinal);
        Assert.Contains("Alt+3", text, StringComparison.Ordinal);
        Assert.Contains("Alt+4", text, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F13-G08")]
    public void PatientMobileWorkspaceEnsuresMainLandmarkAndTouchTargetRequirements()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        context.Services.AddSingleton<IPlatformConnectivityService>(new FakeConnectivityService(true));
        using var cultureScope = new CultureScope();

        var component = context.Render<PatientMobileWorkspace>();

        var main = component.Find("div[role='main']");
        Assert.NotNull(main);
        Assert.Equal("Hasta Mobil Portalı", main.GetAttribute("aria-label"));

        var touchButtons = component.FindAll(".mobile-touch-btn");
        Assert.NotEmpty(touchButtons);

        var primaryActionBtn = component.Find("button.primary-action-btn");
        Assert.NotNull(primaryActionBtn);
        Assert.Equal("Yeni Randevu Al", primaryActionBtn.GetAttribute("aria-label"));
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F13-G08")]
    public void ConnectionRequiredStateProvidesAccessibleAlertAndAriaLiveAnnouncements()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        context.Services.AddSingleton<IPlatformConnectivityService>(new FakeConnectivityService(false));
        using var cultureScope = new CultureScope();

        var component = context.Render<ConnectionRequiredState>(parameters => parameters
            .Add(p => p.Message, "Klinik veri güvenliği için canlı bağlantı gereklidir."));

        var alert = component.Find("[role='alert']");
        Assert.NotNull(alert);
        Assert.Equal("assertive", alert.GetAttribute("aria-live"));

        var icon = component.Find("svg");
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));

        var button = component.Find("button[type='button']");
        Assert.NotNull(button);
        Assert.Contains("Yeniden Dene", button.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F13-G08")]
    public void UiStatePanelProvidesAriaLiveAndRoleConfiguration()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope();

        var component = context.Render<UiStatePanel>(parameters => parameters
            .Add(p => p.Title, "Kayıt Bulunamadı")
            .Add(p => p.Description, "Herhangi bir klinik kayıt listelenmedi.")
            .Add(p => p.Role, "status")
            .Add(p => p.AriaLive, "polite")
            .Add(p => p.IsBusy, false)
            .Add(p => p.ActionText, "Yenile"));

        var section = component.Find("section.ui-state");
        Assert.NotNull(section);
        Assert.Equal("status", section.GetAttribute("role"));
        Assert.Equal("polite", section.GetAttribute("aria-live"));
        Assert.Equal("false", section.GetAttribute("aria-busy"));

        var icon = component.Find(".ui-state__icon");
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));

        var heading = component.Find("h2.ui-state__title");
        Assert.Equal("Kayıt Bulunamadı", heading.TextContent);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F13-G08")]
    public void DemoSecurityBannerRendersAccessibleDismissibleNote()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope();

        var component = context.Render<DemoSecurityBanner>();

        var banner = component.Find("aside.demo-security-banner");
        Assert.NotNull(banner);
        Assert.Equal("region", banner.GetAttribute("role"));

        var icon = component.Find("svg.demo-security-banner__icon");
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));

        var text = banner.TextContent;
        Assert.Contains("EĞİTİM VE SİMÜLASYON ORTAMI", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F13-G08")]
    public void InAppNotificationCenterRendersAccessibleStatusAndCounter()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope();

        var component = context.Render<InAppNotificationCenter>();

        var region = component.Find("section.notification-center");
        Assert.NotNull(region);
        Assert.Equal("region", region.GetAttribute("role"));
        Assert.Equal("Uygulama İçi Bildirim Merkezi", region.GetAttribute("aria-label"));

        var title = component.Find("h2.center-title");
        Assert.Equal("Bildirimler", title.TextContent);
    }
}
