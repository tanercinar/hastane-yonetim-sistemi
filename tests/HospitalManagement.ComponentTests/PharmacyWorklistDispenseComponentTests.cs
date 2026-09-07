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

public sealed class PharmacyWorklistDispenseComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F05-G07")]
    public void PharmacyWorklistOpensDispenseModalWithFefoAndSubmitsDispense()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var rxId = Guid.NewGuid();
        var medId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var stockItemId = Guid.NewGuid();

        var sampleSummary = new PrescriptionSummaryResponse(
            rxId,
            "DEMO-RX-20260829-3001",
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
            "DEMO-RX-20260829-3001",
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
            "Tanı",
            "Talimat",
            1,
            DateTime.UtcNow,
            null,
            [
                new PrescriptionItemResponse(
                    itemId,
                    rxId,
                    medId,
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
                    null),
            ]);

        var sampleDispensed = new PrescriptionDetailResponse(
            rxId,
            "DEMO-RX-20260829-3001",
            Guid.Parse("00000000-0000-0000-0000-000000000109"),
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
            "Tanı",
            "Talimat",
            2,
            DateTime.UtcNow,
            null,
            [
                new PrescriptionItemResponse(
                    itemId,
                    rxId,
                    medId,
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
                    null),
            ]);

        var fefoCandidates = new List<FefoCandidateStockResponse>
        {
            new(
                stockItemId,
                "Merkez Eczane Deposu",
                "LOT-2026-AMX-A",
                DateTime.UtcNow.AddMonths(6),
                50,
                180),
        };

        var handler = new FakePharmacyDispenseHttpHandler(
            worklist: [sampleSummary],
            detail: sampleDetail,
            dispensed: sampleDispensed,
            fefoCandidates: fefoCandidates);

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
            Assert.Contains("DEMO-RX-20260829-3001", component.Markup, StringComparison.Ordinal);
        });

        // 1. Click "İncele"
        var inspectBtn = component.FindAll("button").First(b => b.TextContent.Contains("İncele", StringComparison.Ordinal));
        inspectBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("İlaç Teslim Et", component.Markup, StringComparison.Ordinal);
        });

        // 2. Click "İlaç Teslim Et"
        var dispenseModalBtn = component.FindAll("button").First(b => b.TextContent.Contains("İlaç Teslim Et", StringComparison.Ordinal));
        dispenseModalBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("İlaç Teslimi / Karşılama", component.Markup, StringComparison.Ordinal);
            Assert.Contains("LOT-2026-AMX-A", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Teslimi Onayla ve Stoktan Düş", component.Markup, StringComparison.Ordinal);
        });

        // 3. Click "Teslimi Onayla ve Stoktan Düş"
        var submitBtn = component.FindAll("button").First(b => b.TextContent.Contains("Teslimi Onayla ve Stoktan Düş", StringComparison.Ordinal));
        submitBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Reçete teslimi başarıyla kaydedildi", component.Markup, StringComparison.Ordinal);
        });
    }

    private sealed class FakePharmacyDispenseHttpHandler : HttpMessageHandler
    {
        private readonly List<PrescriptionSummaryResponse> _worklist;
        private readonly PrescriptionDetailResponse _detail;
        private readonly PrescriptionDetailResponse _dispensed;
        private readonly List<FefoCandidateStockResponse> _fefoCandidates;

        public FakePharmacyDispenseHttpHandler(
            List<PrescriptionSummaryResponse> worklist,
            PrescriptionDetailResponse detail,
            PrescriptionDetailResponse dispensed,
            List<FefoCandidateStockResponse> fefoCandidates)
        {
            _worklist = worklist;
            _detail = detail;
            _dispensed = dispensed;
            _fefoCandidates = fefoCandidates;
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
                    "demo-pharmacist@hospital.invalid",
                    "Staff",
                    Guid.NewGuid().ToString(),
                    false,
                    Roles: ["Pharmacist"]);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(account),
                };
            }

            if (path.Contains("identity/antiforgery", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new AntiforgeryTokenResponse("token-123")),
                };
            }

            if (path.Contains("inventory/fefo-candidates", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_fefoCandidates),
                };
            }

            if (path.Contains("dispense", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_dispensed),
                };
            }

            if (path.Contains("prescriptions/worklist", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_worklist),
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
