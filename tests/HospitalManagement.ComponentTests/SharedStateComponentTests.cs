using System.Globalization;
using System.Net;

using HospitalManagement.UI.Components;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HospitalManagement.ComponentTests;

public sealed class SharedStateComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F01-G08")]
    public async Task LoadingStateRendersPoliteBusyStatusWithTurkishDefaultText()
    {
        var markup = await RenderAsync<LoadingState>();
        var visibleText = WebUtility.HtmlDecode(markup);

        Assert.Contains("role=\"status\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"true\"", markup, StringComparison.Ordinal);
        Assert.Contains("Bilgiler yükleniyor", visibleText, StringComparison.Ordinal);
        Assert.DoesNotContain("<button", markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F01-G08")]
    public async Task EmptyStateRendersOnlyConfiguredSafeAction()
    {
        var markup = await RenderAsync<EmptyState>(new Dictionary<string, object?>
        {
            [nameof(EmptyState.ActionText)] = "Genel bakışa dön",
            [nameof(EmptyState.ActionHref)] = "/",
        });
        var visibleText = WebUtility.HtmlDecode(markup);

        Assert.Contains("role=\"status\"", markup, StringComparison.Ordinal);
        Assert.Contains("href=\"/\"", markup, StringComparison.Ordinal);
        Assert.Contains("Genel bakışa dön", visibleText, StringComparison.Ordinal);
        Assert.DoesNotContain("<button", markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F01-G08")]
    public async Task ErrorStateRendersAssertiveAlertWithoutExecutingMarkupFromText()
    {
        const string untrustedTitle = "<script>DEMO-XSS-CANARY</script>";
        var markup = await RenderAsync<ErrorState>(new Dictionary<string, object?>
        {
            [nameof(ErrorState.Title)] = untrustedTitle,
        });

        Assert.Contains("role=\"alert\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"assertive\"", markup, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;DEMO-XSS-CANARY&lt;/script&gt;", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>", markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F01-G08")]
    public async Task ForbiddenStateDoesNotRevealProtectedResourceDetails()
    {
        var markup = await RenderAsync<ForbiddenState>();
        var visibleText = WebUtility.HtmlDecode(markup);

        Assert.Contains("Bu alana erişiminiz yok", visibleText, StringComparison.Ordinal);
        Assert.Contains("kaynak kapsamınız", visibleText, StringComparison.Ordinal);
        Assert.DoesNotContain("PatientId", visibleText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hasta adı", visibleText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", visibleText, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> RenderAsync<TComponent>(
        IDictionary<string, object?>? parameters = null)
        where TComponent : IComponent
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
                var renderedComponent = await renderer.RenderComponentAsync<TComponent>(parameterView);
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
