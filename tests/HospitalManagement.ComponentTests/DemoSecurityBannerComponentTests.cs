using System.Globalization;

using Bunit;

using HospitalManagement.UI.Components;

using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class DemoSecurityBannerComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F02-G07")]
    public void DemoSecurityBannerRendersAccessibleWarningAndDisclaimer()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var component = context.Render<DemoSecurityBanner>();

        var banner = component.Find("aside.demo-security-banner");
        Assert.NotNull(banner);
        Assert.Equal("region", banner.GetAttribute("role"));

        var text = banner.TextContent;
        Assert.Contains("EĞİTİM VE SİMÜLASYON ORTAMI", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Gerçek hasta verisi", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HBYS", text, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
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
}
