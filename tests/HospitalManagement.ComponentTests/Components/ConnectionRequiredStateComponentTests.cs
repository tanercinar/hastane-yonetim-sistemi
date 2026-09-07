using System.Globalization;
using System.Net;
using HospitalManagement.UI.Components;
using HospitalManagement.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HospitalManagement.ComponentTests.Components;

public sealed class ConnectionRequiredStateComponentTests
{
    private sealed class FakeConnectivityService(bool isOnline = false) : IPlatformConnectivityService
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

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F12-G03")]
    public async Task ConnectionRequiredStateRendersAssertiveAlertWithDefaultOfflineNotice()
    {
        var markup = await RenderAsync<ConnectionRequiredState>(new FakeConnectivityService(false));
        var visibleText = WebUtility.HtmlDecode(markup);

        Assert.Contains("role=\"alert\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"assertive\"", markup, StringComparison.Ordinal);
        Assert.Contains("Bağlantı Gerekli", visibleText, StringComparison.Ordinal);
        Assert.Contains("Klinik veri güvenliği ve hasta güvenliği gereği sistem çevrimdışı çalışmayı desteklemez", visibleText, StringComparison.Ordinal);
        Assert.Contains("Yeniden Dene", visibleText, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F12-G03")]
    public async Task ConnectionRequiredStateRendersCustomMessageWhenProvided()
    {
        const string customMsg = "Özel bağlantı hatası: Sunucuya ulaşılamıyor.";
        var markup = await RenderAsync<ConnectionRequiredState>(
            new FakeConnectivityService(false),
            new Dictionary<string, object?>
            {
                [nameof(ConnectionRequiredState.Message)] = customMsg
            });
        var visibleText = WebUtility.HtmlDecode(markup);

        Assert.Contains(customMsg, visibleText, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync<TComponent>(
        IPlatformConnectivityService connectivityService,
        IDictionary<string, object?>? parameters = null)
        where TComponent : IComponent
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.AddSingleton(connectivityService);

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
