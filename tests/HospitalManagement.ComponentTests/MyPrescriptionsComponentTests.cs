using System.Globalization;
using System.Net;
using System.Net.Http.Json;

using Bunit;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Pharmacy;
using HospitalManagement.UI.Components;
using HospitalManagement.Web.Client.Identity;
using HospitalManagement.Web.Client.Pages.Patient;
using HospitalManagement.Web.Client.Pharmacy;

using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class MyPrescriptionsComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F05-G08")]
    public void MyPrescriptionsRendersForPatientWithDisclaimerAndList()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var rxId = Guid.NewGuid();

        var sampleSummary = new PrescriptionSummaryResponse(
            rxId,
            "DEMO-RX-20260829-4001",
            patientId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Signed",
            DateTime.UtcNow.AddDays(14),
            DateTime.UtcNow,
            1,
            DateTime.UtcNow);

        var handler = new FakePatientPrescriptionsHttpHandler(
            patientId: patientId,
            userRole: "Patient",
            prescriptions: [sampleSummary]);

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var pharmacyClient = new PharmacyApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<IPharmacyApiClient>(pharmacyClient);

        var component = context.Render<MyPrescriptions>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Reçetelerim", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Hasta Görünümü", component.Markup, StringComparison.Ordinal);
            Assert.Contains("EĞİTİM VE SİMÜLASYON ORTAMI", component.Markup, StringComparison.Ordinal);
            Assert.Contains("DEMO-RX-20260829-4001", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Geçerli / Eczaneden Alınabilir", component.Markup, StringComparison.Ordinal);
            Assert.Contains("1 kalem", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F05-G08")]
    public void MyPrescriptionsOpensDetailModalWithFriendlyJargon()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var rxId = Guid.NewGuid();

        var sampleSummary = new PrescriptionSummaryResponse(
            rxId,
            "DEMO-RX-20260829-4002",
            patientId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Dispensed",
            DateTime.UtcNow.AddDays(14),
            DateTime.UtcNow,
            1,
            DateTime.UtcNow);

        var sampleDetail = new PrescriptionDetailResponse(
            rxId,
            "DEMO-RX-20260829-4002",
            patientId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Dispensed",
            DateTime.UtcNow.AddDays(14),
            DateTime.UtcNow,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            "Üst Solunum Yolu Enfeksiyonu",
            "Günde 2 litre su içiniz.",
            2,
            DateTime.UtcNow,
            null,
            [
                new PrescriptionItemResponse(
                    Guid.NewGuid(),
                    rxId,
                    Guid.NewGuid(),
                    "DEMO-MED-AMX500",
                    "DEMO-Amoksilin 500mg Kapsül",
                    "Amoksisilin",
                    "Capsule",
                    "Oral",
                    500,
                    "mg",
                    "2x1",
                    7,
                    1,
                    "kutu",
                    1,
                    true,
                    "Yemeklerden sonra içiniz."),
            ]);

        var handler = new FakePatientPrescriptionsHttpHandler(
            patientId: patientId,
            userRole: "Patient",
            prescriptions: [sampleSummary],
            detail: sampleDetail);

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var pharmacyClient = new PharmacyApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<IPharmacyApiClient>(pharmacyClient);

        var component = context.Render<MyPrescriptions>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("DEMO-RX-20260829-4002", component.Markup, StringComparison.Ordinal);
        });

        // Click "İlaçları ve Talimatları Gör"
        var viewBtn = component.FindAll("button").First(b => b.TextContent.Contains("İlaçları ve Talimatları Gör", StringComparison.Ordinal));
        viewBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Reçete: DEMO-RX-20260829-4002", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Günde 2 litre su içiniz.", component.Markup, StringComparison.Ordinal);
            Assert.Contains("DEMO-Amoksilin 500mg Kapsül", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Ağızdan", component.Markup, StringComparison.Ordinal);
            Assert.Contains("500 mg — günde 2 kez", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Eczaneden Alındı", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Yemeklerden sonra içiniz.", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F05-G08")]
    public void MyPrescriptionsForbiddenForStaffUser()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var handler = new FakePatientPrescriptionsHttpHandler(userRole: "Doctor");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var pharmacyClient = new PharmacyApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<IPharmacyApiClient>(pharmacyClient);

        var component = context.Render<MyPrescriptions>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Bu alana erişiminiz yok", component.Markup, StringComparison.Ordinal);
        });
    }

    private sealed class FakePatientPrescriptionsHttpHandler : HttpMessageHandler
    {
        private readonly Guid _patientId;
        private readonly string _userRole;
        private readonly List<PrescriptionSummaryResponse>? _prescriptions;
        private readonly PrescriptionDetailResponse? _detail;

        public FakePatientPrescriptionsHttpHandler(
            Guid? patientId = null,
            string userRole = "Patient",
            List<PrescriptionSummaryResponse>? prescriptions = null,
            PrescriptionDetailResponse? detail = null)
        {
            _patientId = patientId ?? Guid.NewGuid();
            _userRole = userRole;
            _prescriptions = prescriptions;
            _detail = detail;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.Contains("identity/session", StringComparison.OrdinalIgnoreCase))
            {
                var account = new CurrentAccountResponse(
                    "demo-patient@hospital.invalid",
                    _userRole == "Patient" ? "Patient" : "Staff",
                    _patientId.ToString(),
                    false,
                    Roles: [_userRole]);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(account),
                };
            }

            if (path.Contains("prescriptions/by-patient", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_prescriptions ?? []),
                };
            }

            if (path.Contains("prescriptions/", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_detail),
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
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
