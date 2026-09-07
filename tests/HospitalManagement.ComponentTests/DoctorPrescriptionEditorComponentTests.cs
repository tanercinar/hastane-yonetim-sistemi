using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Bunit;
using HospitalManagement.Contracts.ClinicalRecords;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Pharmacy;
using HospitalManagement.UI.Components;
using HospitalManagement.Web.Client.Identity;
using HospitalManagement.Web.Client.Pages.Doctor;
using HospitalManagement.Web.Client.Pharmacy;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class DoctorPrescriptionEditorComponentTests
{
    private static readonly Guid TestEncounterId = Guid.Parse("70000000-0000-0000-0000-000000000001");
    private static readonly Guid TestPatientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
    private static readonly Guid TestDepartmentId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly Guid TestDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F05-G03")]
    public void DoctorPrescriptionEditorRendersDraftFormAndCatalogSearch()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var handler = new FakePharmacyHttpHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var pharmacyClient = new PharmacyApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<IPharmacyApiClient>(pharmacyClient);

        var component = context.Render<DoctorPrescriptionEditor>(parameters =>
            parameters.Add(page => page.EncounterId, TestEncounterId));

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Doktor Reçete Yönetimi", component.Markup, StringComparison.Ordinal);
            Assert.Contains("İlaç Kataloğunda Ara", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Reçete Kalemleri (0)", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Taslak Olarak Kaydet", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Reçeteyi İmzala", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F05-G03")]
    public void DoctorSearchesMedicationAndAddsToDraft()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var sampleMed = new MedicationCatalogItemResponse(
            Guid.Parse("00000000-0000-0000-0000-000000000601"),
            "DEMO-MED-AMX500",
            "DEMO-Amoksilin 500mg Kapsül",
            "Amoksisilin",
            "Capsule",
            500m,
            "mg",
            "Oral",
            "J01CA04",
            "Geniş spektrumlu antibiyotik",
            "DEMO-MED-2026.1",
            true,
            DateTime.UtcNow);

        var handler = new FakePharmacyHttpHandler(searchResults: [sampleMed]);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var pharmacyClient = new PharmacyApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<IPharmacyApiClient>(pharmacyClient);

        var component = context.Render<DoctorPrescriptionEditor>(parameters =>
            parameters.Add(page => page.EncounterId, TestEncounterId));

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Doktor Reçete Yönetimi", component.Markup, StringComparison.Ordinal);
            Assert.Contains("İlaç Kataloğunda Ara", component.Markup, StringComparison.Ordinal);
        });

        // Type search query and click Ara
        var searchInput = component.Find("input[placeholder*='İlaç adı']");
        searchInput.Input("Amoksisilin");

        var searchBtn = component.Find("button.ui-button--primary");
        searchBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("DEMO-Amoksilin 500mg Kapsül", component.Markup, StringComparison.Ordinal);
            Assert.Contains("+ Ekle", component.Markup, StringComparison.Ordinal);
        });

        // Click + Ekle
        var addBtn = component.FindAll("button").First(b => b.TextContent.Contains("+ Ekle", StringComparison.Ordinal));
        addBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Reçete Kalemleri (1)", component.Markup, StringComparison.Ordinal);
            Assert.Contains("DEMO-MED-AMX500", component.Markup, StringComparison.Ordinal);
            Assert.Contains("'DEMO-Amoksilin 500mg Kapsül' reçeteye eklendi.", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F05-G03")]
    public void DoctorSavesDraftAndSignsPrescription()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var sampleMed = new MedicationCatalogItemResponse(
            Guid.Parse("00000000-0000-0000-0000-000000000604"),
            "DEMO-MED-PAR500",
            "DEMO-Parasetamol 500mg",
            "Parasetamol",
            "Tablet",
            500m,
            "mg",
            "Oral",
            "N02BE01",
            null,
            "DEMO-MED-2026.1",
            true,
            DateTime.UtcNow);

        var rxId = Guid.NewGuid();
        var draftDetail = new PrescriptionDetailResponse(
            rxId,
            "DEMO-RX-20260829-ABCD12",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Draft",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "Akut Ağrı",
            "Bol sıvı",
            1,
            DateTime.UtcNow,
            null,
            [
                new PrescriptionItemResponse(
                    Guid.NewGuid(),
                    rxId,
                    sampleMed.Id,
                    sampleMed.Code,
                    sampleMed.BrandName,
                    sampleMed.GenericName,
                    sampleMed.Form,
                    sampleMed.Route,
                    500m,
                    "mg",
                    "1x1",
                    5,
                    1,
                    "kutu",
                    0,
                    false,
                    null),
            ]);

        var signedDetail = draftDetail with
        {
            Status = "Signed",
            SignedAtUtc = DateTime.UtcNow,
            ValidUntilUtc = DateTime.UtcNow.AddDays(14),
            Version = 2,
        };

        var handler = new FakePharmacyHttpHandler(
            searchResults: [sampleMed],
            createDraftResponse: draftDetail,
            signResponse: signedDetail);

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var pharmacyClient = new PharmacyApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<IPharmacyApiClient>(pharmacyClient);

        var component = context.Render<DoctorPrescriptionEditor>(parameters =>
            parameters.Add(page => page.EncounterId, TestEncounterId));

        component.WaitForAssertion(() =>
        {
            Assert.Contains("İlaç Kataloğunda Ara", component.Markup, StringComparison.Ordinal);
        });

        // Add item
        var searchInput = component.Find("input[placeholder*='İlaç adı']");
        searchInput.Input("Parasetamol");
        var searchBtn = component.Find("button.ui-button--primary");
        searchBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("+ Ekle", component.Markup, StringComparison.Ordinal);
        });

        var addBtn = component.FindAll("button").First(b => b.TextContent.Contains("+ Ekle", StringComparison.Ordinal));
        addBtn.Click();

        // Click "Taslak Olarak Kaydet"
        var saveDraftBtn = component.FindAll("button").First(b => b.TextContent.Contains("Taslak Olarak Kaydet", StringComparison.Ordinal));
        saveDraftBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("DEMO-RX-20260829-ABCD12", component.Markup, StringComparison.Ordinal);
        });

        // Click "Reçeteyi İmzala"
        var signBtn = component.FindAll("button").First(b => b.TextContent.Contains("Reçeteyi İmzala", StringComparison.Ordinal));
        signBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("İmzalı (Signed)", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Klinik Değişmezlik Bildirimi", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Reçeteyi İptal Et", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F05-G03")]
    public void DoctorPrescriptionEditorRendersForbiddenForNonDoctor()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var handler = new FakePharmacyHttpHandler(userRole: "Patient");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var pharmacyClient = new PharmacyApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<IPharmacyApiClient>(pharmacyClient);

        var component = context.Render<DoctorPrescriptionEditor>(parameters =>
            parameters.Add(page => page.EncounterId, TestEncounterId));

        component.WaitForAssertion(() =>
        {
            Assert.NotNull(component.FindComponent<ForbiddenState>());
            Assert.Contains("Genel Bakışa Dön", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F05-G04")]
    public void DoctorEncounteringSafetyWarningCanSignWithOverrideReason()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var sampleMed = new MedicationCatalogItemResponse(
            Guid.Parse("00000000-0000-0000-0000-000000000601"),
            "DEMO-MED-AMX500",
            "DEMO-Amoksilin 500mg Kapsül",
            "Amoksisilin",
            "Capsule",
            500m,
            "mg",
            "Oral",
            "J01CA04",
            "Geniş spektrumlu antibiyotik",
            "DEMO-MED-2026.1",
            true,
            DateTime.UtcNow);

        var rxId = Guid.NewGuid();
        var draftDetail = new PrescriptionDetailResponse(
            rxId,
            "DEMO-RX-20260829-SAFE01",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Draft",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "Akut Tonsillit",
            "Günde 2 kez",
            1,
            DateTime.UtcNow,
            null,
            [
                new PrescriptionItemResponse(
                    Guid.NewGuid(),
                    rxId,
                    sampleMed.Id,
                    sampleMed.Code,
                    sampleMed.BrandName,
                    sampleMed.GenericName,
                    sampleMed.Form,
                    sampleMed.Route,
                    500m,
                    "mg",
                    "2x1",
                    7,
                    1,
                    "kutu",
                    0,
                    false,
                    null),
            ]);

        var signedDetail = draftDetail with
        {
            Status = "Signed",
            SignedAtUtc = DateTime.UtcNow,
            ValidUntilUtc = DateTime.UtcNow.AddDays(14),
            Version = 2,
        };

        var safetyResponse = new MedicationSafetyCheckResponse(
            HasWarnings: true,
            HasCriticalWarnings: true,
            Warnings:
            [
                new MedicationSafetyWarningResponse(
                    "DEMO-WARN-ALLERGY-DEMO-MED-AMX500",
                    "AllergyCrossReaction",
                    "Critical",
                    "Alerji Çapraz Reaksiyon Uyarısı: Penisilin",
                    "Hastanın aktif Penisilin alerji kaydı bulunmaktadır.",
                    "DEMO-Amoksilin 500mg Kapsül",
                    "Penisilin",
                    RequiresOverrideReason: true),
            ],
            Disclaimer: "DİKKAT: Bu güvenlik uyarıları sentetik DEMO verilerle kural tabanlı deterministik olarak üretilmiştir.");

        var handler = new FakePharmacyHttpHandler(
            searchResults: [sampleMed],
            createDraftResponse: draftDetail,
            signResponse: signedDetail,
            safetyResponse: safetyResponse);

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var pharmacyClient = new PharmacyApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<IPharmacyApiClient>(pharmacyClient);

        var component = context.Render<DoctorPrescriptionEditor>(parameters =>
            parameters.Add(page => page.EncounterId, TestEncounterId));

        component.WaitForAssertion(() =>
        {
            Assert.Contains("İlaç Kataloğunda Ara", component.Markup, StringComparison.Ordinal);
        });

        // Add medication
        var searchInput = component.Find("input[placeholder*='İlaç adı']");
        searchInput.Input("Amoksisilin");
        var searchBtn = component.Find("button.ui-button--primary");
        searchBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("+ Ekle", component.Markup, StringComparison.Ordinal);
        });

        var addBtn = component.FindAll("button").First(b => b.TextContent.Contains("+ Ekle", StringComparison.Ordinal));
        addBtn.Click();

        // Check that safety warning panel is rendered
        component.WaitForAssertion(() =>
        {
            Assert.Contains("Kural Tabanlı Güvenlik Uyarıları", component.Markup, StringComparison.Ordinal);
            Assert.Contains("KRİTİK UYARI", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Alerji Çapraz Reaksiyon Uyarısı: Penisilin", component.Markup, StringComparison.Ordinal);
        });

        // Click Reçeteyi İmzala -> opens override modal
        var signBtn = component.FindAll("button").First(b => b.TextContent.Contains("Reçeteyi İmzala", StringComparison.Ordinal));
        signBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Güvenlik Uyarıları — Geçersiz Kılma Gerekçesi", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Geçersiz Kılma Gerekçesi", component.Markup, StringComparison.Ordinal);
        });

        // Fill override reason
        var reasonArea = component.Find("textarea#overrideReasonInput");
        reasonArea.Change("Fayda-risk değerlendirmesi yapıldı, hasta bilgilendirildi.");

        // Click Gerekçeli İmzala
        var confirmBtn = component.FindAll("button").First(b => b.TextContent.Contains("Gerekçeli İmzala", StringComparison.Ordinal));
        confirmBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("İmzalı (Signed)", component.Markup, StringComparison.Ordinal);
            Assert.Contains("DEMO-RX-20260829-SAFE01", component.Markup, StringComparison.Ordinal);
        });
    }

    private sealed class FakePharmacyHttpHandler : HttpMessageHandler
    {
        private readonly List<MedicationCatalogItemResponse>? _searchResults;
        private readonly PrescriptionDetailResponse? _createDraftResponse;
        private readonly PrescriptionDetailResponse? _signResponse;
        private readonly MedicationSafetyCheckResponse? _safetyResponse;
        private readonly string _userRole;

        public FakePharmacyHttpHandler(
            List<MedicationCatalogItemResponse>? searchResults = null,
            PrescriptionDetailResponse? createDraftResponse = null,
            PrescriptionDetailResponse? signResponse = null,
            MedicationSafetyCheckResponse? safetyResponse = null,
            string userRole = "Doctor")
        {
            _searchResults = searchResults;
            _createDraftResponse = createDraftResponse;
            _signResponse = signResponse;
            _safetyResponse = safetyResponse;
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
                    _userRole == "Doctor" ? "Staff" : "Patient",
                    Guid.NewGuid().ToString(),
                    false,
                    Roles: [_userRole]);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(account),
                };
            }

            if (path.Contains("identity/antiforgery", StringComparison.OrdinalIgnoreCase))
            {
                var token = new AntiforgeryTokenResponse("fake-csrf-token");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(token),
                };
            }

            if (path.Contains("clinical-records/encounters/", StringComparison.OrdinalIgnoreCase))
            {
                var encounter = new EncounterDetailResponse(
                    TestEncounterId,
                    null,
                    TestPatientId,
                    TestDepartmentId,
                    TestDoctorId,
                    "Outpatient",
                    "InProgress",
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    1,
                    DateTime.UtcNow,
                    null,
                    []);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(encounter),
                };
            }

            if (path.Contains("pharmacy/prescriptions/safety-check", StringComparison.OrdinalIgnoreCase))
            {
                var response = _safetyResponse ?? new MedicationSafetyCheckResponse(
                    false, false, [], "DİKKAT: Bu güvenlik uyarıları sentetik DEMO verilerle kural tabanlı deterministik olarak üretilmiştir.");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response),
                };
            }

            if (path.Contains("pharmacy/medications", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_searchResults ?? []),
                };
            }

            if (path.Contains("/sign", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_signResponse ?? _createDraftResponse),
                };
            }

            if (request.Method == HttpMethod.Post && path.EndsWith("pharmacy/prescriptions", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = JsonContent.Create(_createDraftResponse),
                };
            }

            if (request.Method == HttpMethod.Put && path.Contains("pharmacy/prescriptions", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_createDraftResponse),
                };
            }

            if (path.Contains("pharmacy/prescriptions/by-encounter", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new List<PrescriptionSummaryResponse>()),
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
