using System.Globalization;
using System.Net;
using HospitalManagement.UI.Components.PatientMobile;
using HospitalManagement.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HospitalManagement.ComponentTests.Components;

public sealed class PatientMobileWorkspaceComponentTests
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
    [Trait("Roadmap", "F12-G05")]
    public async Task PatientMobileWorkspaceRendersAppointmentsAndDemoBanner()
    {
        var appointments = new List<PatientMobileAppointmentDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AppointmentNumber = "DEMO-APT-20260904-001",
                DepartmentName = "Kardiyoloji",
                DoctorName = "Ahmet Yılmaz",
                ScheduledDate = DateTime.UtcNow.AddDays(2),
                TimeSlot = "10:30",
                Reason = "Göğüs ağrısı kontrolü",
                Status = "Scheduled"
            }
        };

        var markup = await RenderAsync(
            new FakeConnectivityService(true),
            new Dictionary<string, object?>
            {
                [nameof(PatientMobileWorkspace.Appointments)] = appointments
            });
        var visibleText = WebUtility.HtmlDecode(markup);

        Assert.Contains("DEMO ORTAM", visibleText, StringComparison.Ordinal);
        Assert.Contains("Kardiyoloji", visibleText, StringComparison.Ordinal);
        Assert.Contains("Ahmet Yılmaz", visibleText, StringComparison.Ordinal);
        Assert.Contains("DEMO-APT-20260904-001", visibleText, StringComparison.Ordinal);
        Assert.Contains("10:30", visibleText, StringComparison.Ordinal);
        Assert.Contains("Randevularım", visibleText, StringComparison.Ordinal);
        Assert.Contains("Reçeteler", visibleText, StringComparison.Ordinal);
        Assert.Contains("Sonuçlar", visibleText, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F12-G05")]
    public async Task PatientMobileWorkspaceRendersOfflineBarrierWhenDisconnected()
    {
        var markup = await RenderAsync(
            new FakeConnectivityService(false));
        var visibleText = WebUtility.HtmlDecode(markup);

        Assert.Contains("İnternet Bağlantısı Kesildi", visibleText, StringComparison.Ordinal);
        Assert.Contains("Klinik veri güvenliği için canlı bağlantı gereklidir", visibleText, StringComparison.Ordinal);
        Assert.Contains("Tekrar Dene", visibleText, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F12-G05")]
    public void PatientMobileWorkspaceBackNavigationHandlesDetailsGracefully()
    {
        var workspace = new PatientMobileWorkspace();
        Assert.False(workspace.CanGoBack);
        Assert.False(workspace.HandleHardwareBack());

        workspace.SelectedPrescription = new PatientMobilePrescriptionDto
        {
            Id = Guid.NewGuid(),
            PrescriptionNumber = "DEMO-RX-001",
            DoctorName = "Fatma Kaya",
            Diagnosis = "Hipertansiyon"
        };

        Assert.True(workspace.CanGoBack);
        var handled = workspace.HandleHardwareBack();
        Assert.True(handled);
        Assert.Null(workspace.SelectedPrescription);
        Assert.False(workspace.CanGoBack);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F12-G05")]
    public void PatientMobileWorkspaceTouchTargetsAndAriaAttributesConfigured()
    {
        var workspace = new PatientMobileWorkspace();
        workspace.SetActiveTab("prescriptions");
        Assert.Equal("prescriptions", workspace.ActiveTab);
        Assert.Equal("Reçetelerim", workspace.GetActiveTitle());

        workspace.SetActiveTab("results");
        Assert.Equal("results", workspace.ActiveTab);
        Assert.Equal("Tahlil & Sonuçlarım", workspace.GetActiveTitle());

        workspace.ActiveCategoryFilter = "Laboratory";
        workspace.SetDiagnosticResults(
        [
            new PatientMobileDiagnosticResultDto
            {
                Id = Guid.NewGuid(),
                RequestNumber = "DEMO-LAB-01",
                Category = "Laboratory",
                TestName = "Tam Kan Sayımı (Hemogram)",
                Status = "Final",
                ResultSummary = "WBC: 6.8, Hb: 14.2",
                IsCritical = false
            },
            new PatientMobileDiagnosticResultDto
            {
                Id = Guid.NewGuid(),
                RequestNumber = "DEMO-RAD-01",
                Category = "Radiology",
                TestName = "Akciğer Grafisi",
                Status = "Final",
                ResultSummary = "Normal",
                IsCritical = false
            }
        ]);

        var filtered = workspace.FilteredResults;
        Assert.Single(filtered);
        Assert.Equal("Laboratory", filtered[0].Category);
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
                var renderedComponent = await renderer.RenderComponentAsync<PatientMobileWorkspace>(parameterView);
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
