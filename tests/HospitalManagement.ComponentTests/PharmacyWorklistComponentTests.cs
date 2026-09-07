using System.Globalization;
using System.Net;
using System.Net.Http.Json;

using Bunit;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Pharmacy;
using HospitalManagement.UI.Components;
using HospitalManagement.Web.Client.Identity;
using HospitalManagement.Web.Client.Pages.Pharmacy;
using HospitalManagement.Web.Client.Pharmacy;

using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class PharmacyWorklistComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F05-G05")]
    public void PharmacyWorklistRendersForPharmacistWithFiltersAndList()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var sampleRx = new PrescriptionSummaryResponse(
            Guid.NewGuid(),
            "DEMO-RX-20260829-0001",
            Guid.Parse("00000000-0000-0000-0000-000000000109"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Signed",
            DateTime.UtcNow.AddDays(14),
            DateTime.UtcNow,
            2,
            DateTime.UtcNow);

        var handler = new FakePharmacyWorklistHttpHandler(worklist: [sampleRx], userRole: "Pharmacist");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var pharmacyClient = new PharmacyApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<IPharmacyApiClient>(pharmacyClient);

        var component = context.Render<PharmacyWorklist>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Eczane İş Listesi", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Eczacı Portalı", component.Markup, StringComparison.Ordinal);
            Assert.Contains("DEMO-RX-20260829-0001", component.Markup, StringComparison.Ordinal);
            Assert.Contains("İmzalı (Signed)", component.Markup, StringComparison.Ordinal);
            Assert.Contains("2 kalem", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F05-G05")]
    public void PharmacyWorklistOpensDetailModalWithClinicalIsolation()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var rxId = Guid.NewGuid();
        var sampleSummary = new PrescriptionSummaryResponse(
            rxId,
            "DEMO-RX-20260829-0002",
            Guid.Parse("00000000-0000-0000-0000-000000000109"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Signed",
            DateTime.UtcNow.AddDays(14),
            DateTime.UtcNow,
            1,
            DateTime.UtcNow);

        var sampleDetail = new PrescriptionDetailResponse(
            rxId,
            "DEMO-RX-20260829-0002",
            Guid.Parse("00000000-0000-0000-0000-000000000109"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Signed",
            DateTime.UtcNow.AddDays(14),
            DateTime.UtcNow,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            "Akut Tonsillit",
            "Bol sıvı ve dinlenme",
            1,
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
                    0,
                    false,
                    "Yemeklerden sonra"),
            ]);

        var handler = new FakePharmacyWorklistHttpHandler(
            worklist: [sampleSummary],
            detail: sampleDetail,
            userRole: "Pharmacist");

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var pharmacyClient = new PharmacyApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<IPharmacyApiClient>(pharmacyClient);

        var component = context.Render<PharmacyWorklist>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("DEMO-RX-20260829-0002", component.Markup, StringComparison.Ordinal);
        });

        // Click "İncele"
        var inspectBtn = component.FindAll("button").First(b => b.TextContent.Contains("İncele", StringComparison.Ordinal));
        inspectBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Reçete Detayı: DEMO-RX-20260829-0002", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Klinik İzolasyon Uyumu", component.Markup, StringComparison.Ordinal);
            Assert.Contains("DEMO-Amoksilin 500mg Kapsül", component.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Akut Tonsillit", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Tanı, düzeltme gerekçeleri ve karşılaşma notları", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Kapat", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F05-G05")]
    public void PharmacyWorklistForbiddenForUnauthorizedUser()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var handler = new FakePharmacyWorklistHttpHandler(userRole: "Patient");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var pharmacyClient = new PharmacyApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<IPharmacyApiClient>(pharmacyClient);

        var component = context.Render<PharmacyWorklist>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Bu alana erişiminiz yok", component.Markup, StringComparison.Ordinal);
        });
    }

    private sealed class FakePharmacyWorklistHttpHandler : HttpMessageHandler
    {
        private readonly List<PrescriptionSummaryResponse>? _worklist;
        private readonly PrescriptionDetailResponse? _detail;
        private readonly string _userRole;

        public FakePharmacyWorklistHttpHandler(
            List<PrescriptionSummaryResponse>? worklist = null,
            PrescriptionDetailResponse? detail = null,
            string userRole = "Pharmacist")
        {
            _worklist = worklist;
            _detail = detail;
            _userRole = userRole;
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
                    "demo-user@hospital.invalid",
                    _userRole == "Patient" ? "Patient" : "Staff",
                    Guid.NewGuid().ToString(),
                    false,
                    Roles: [_userRole]);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(account),
                };
            }

            if (path.Contains("pharmacy/prescriptions/worklist", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_worklist ?? []),
                };
            }

            if (path.Contains("pharmacy/prescriptions/", StringComparison.OrdinalIgnoreCase))
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
