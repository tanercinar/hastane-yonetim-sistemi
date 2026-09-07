using System.Globalization;
using System.Net;
using HospitalManagement.UI.Components.StaffWorkspace;
using HospitalManagement.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HospitalManagement.ComponentTests.Components;

public sealed class StaffDesktopWorkspaceComponentTests
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

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F12-G04")]
    public async Task StaffDesktopWorkspaceRendersForbiddenStateWhenUnauthorized()
    {
        var markup = await RenderAsync(
            new FakeConnectivityService(true),
            new Dictionary<string, object?>
            {
                [nameof(StaffDesktopWorkspace.HasPermission)] = false
            });
        var visibleText = WebUtility.HtmlDecode(markup);

        Assert.Contains("Bu alana erişiminiz yok", visibleText, StringComparison.Ordinal);
        Assert.DoesNotContain("Personel Klinik Çalışma Alanı", visibleText, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F12-G04")]
    public async Task StaffDesktopWorkspaceRendersConnectionRequiredWhenOffline()
    {
        var markup = await RenderAsync(
            new FakeConnectivityService(false),
            new Dictionary<string, object?>
            {
                [nameof(StaffDesktopWorkspace.HasPermission)] = true
            });
        var visibleText = WebUtility.HtmlDecode(markup);

        Assert.Contains("Bağlantı Gerekli", visibleText, StringComparison.Ordinal);
        Assert.DoesNotContain("Günlük Randevu Listesi", visibleText, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F12-G04")]
    public async Task StaffDesktopWorkspaceRendersAppointmentsAndKeyboardShortcuts()
    {
        var apts = new List<StaffAppointmentItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PatientNameMasked = "A*** Y***",
                DepartmentName = "Kardiyoloji",
                DoctorName = "Dr. Ahmet",
                Status = "Planlandı"
            },
            new()
            {
                Id = Guid.NewGuid(),
                PatientNameMasked = "M*** K***",
                DepartmentName = "Dahiliye",
                DoctorName = "Dr. Ayşe",
                Status = "Geldi"
            }
        };

        var markup = await RenderAsync(
            new FakeConnectivityService(true),
            new Dictionary<string, object?>
            {
                [nameof(StaffDesktopWorkspace.HasPermission)] = true,
                [nameof(StaffDesktopWorkspace.StaffRole)] = "Doctor",
                [nameof(StaffDesktopWorkspace.Appointments)] = apts
            });
        var visibleText = WebUtility.HtmlDecode(markup);

        Assert.Contains("Personel Klinik Çalışma Alanı", visibleText, StringComparison.Ordinal);
        Assert.Contains("Alt+1 (Randevular)", visibleText, StringComparison.Ordinal);
        Assert.Contains("A*** Y***", visibleText, StringComparison.Ordinal);
        Assert.Contains("M*** K***", visibleText, StringComparison.Ordinal);
        Assert.Contains("Kardiyoloji", visibleText, StringComparison.Ordinal);
        Assert.Contains("Planlandı", visibleText, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync(
        IPlatformConnectivityService connectivityService,
        IDictionary<string, object?>? parameters = null)
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
                var renderedComponent = await renderer.RenderComponentAsync<StaffDesktopWorkspace>(parameterView);
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
