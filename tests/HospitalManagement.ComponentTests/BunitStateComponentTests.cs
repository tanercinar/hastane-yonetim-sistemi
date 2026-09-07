using System.Globalization;

using Bunit;

using HospitalManagement.UI.Components;

using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class BunitStateComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F01-G10")]
    public void EmptyStateExposesAccessibleStatusAndInvokesConfiguredAction()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");
        var actionInvoked = false;

        var component = context.Render<EmptyState>(parameters => parameters
            .Add(state => state.Title, "DEMO boş durum")
            .Add(state => state.Description, "DEMO kayıt bulunamadı")
            .Add(state => state.ActionText, "Yeniden dene")
            .Add(state => state.OnAction, () => actionInvoked = true));

        var status = component.Find("[role='status']");
        Assert.Equal("polite", status.GetAttribute("aria-live"));
        Assert.Equal("DEMO boş durum", component.Find("h2").TextContent);

        component.Find("button").Click();

        Assert.True(actionInvoked);
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
