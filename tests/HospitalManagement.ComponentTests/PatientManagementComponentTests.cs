using System.Globalization;
using System.Net;
using System.Net.Http.Json;

using Bunit;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Patients;
using HospitalManagement.Web.Client.Identity;
using HospitalManagement.Web.Client.Pages.Patients;
using HospitalManagement.Web.Client.Patients;

using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class PatientManagementComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F03-G02")]
    public void PatientSearchRendersTableWhenLoaded()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var samplePatients = new PatientListResponse(
            [
                new PatientSummaryResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "MRN-2026-000001",
                    "Ayşe",
                    "Yılmaz",
                    new DateOnly(1985, 4, 15),
                    "Female",
                    "99*******01",
                    "+905*******01",
                    "DEMO-ayse@hospital.invalid",
                    IsActive: true,
                    CreatedAtUtc: DateTime.UtcNow),
                new PatientSummaryResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "MRN-2026-000002",
                    "Mehmet",
                    "Kaya",
                    new DateOnly(1978, 10, 20),
                    "Male",
                    "99*******02",
                    "+905*******02",
                    "DEMO-mehmet@hospital.invalid",
                    IsActive: true,
                    CreatedAtUtc: DateTime.UtcNow),
            ],
            TotalCount: 2,
            Page: 1,
            PageSize: 50);

        var handler = new FakePatientHttpHandler(samplePatients);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var patientClient = new PatientApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<IPatientApiClient>(patientClient);

        var component = context.Render<PatientSearch>();

        Assert.Contains("Hasta Arama ve Kayıt Personeli Ekranı", component.Markup, StringComparison.Ordinal);
        Assert.Contains("MRN-2026-000001", component.Markup, StringComparison.Ordinal);
        Assert.Contains("Ayşe Yılmaz", component.Markup, StringComparison.Ordinal);
        Assert.Contains("MRN-2026-000002", component.Markup, StringComparison.Ordinal);
        Assert.Contains("Mehmet Kaya", component.Markup, StringComparison.Ordinal);
        Assert.Contains("Yeni Hasta Kaydı", component.Markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F03-G02")]
    public void PatientSearchRendersForbiddenStateWhenAccessDenied()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var handler = new FakePatientHttpHandler(statusCode: HttpStatusCode.Forbidden);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var patientClient = new PatientApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<IPatientApiClient>(patientClient);

        var component = context.Render<PatientSearch>();
        var nav = context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();

        component.WaitForAssertion(() =>
            Assert.Contains("account/login", nav.Uri, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F03-G02")]
    public void PatientSearchOpensRegistrationModalWhenButtonClicked()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var emptyPatients = new PatientListResponse([], TotalCount: 0, Page: 1, PageSize: 50);

        var handler = new FakePatientHttpHandler(emptyPatients);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var patientClient = new PatientApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<IPatientApiClient>(patientClient);

        var component = context.Render<PatientSearch>();

        component.WaitForAssertion(() =>
            Assert.Contains("Hasta Arama ve Kayıt Personeli Ekranı", component.Markup, StringComparison.Ordinal));

        // Click "Yeni Hasta Kaydı" button
        var button = component.Find("button.ui-button--primary");
        button.Click();

        // Modal should now be open
        component.WaitForAssertion(() =>
        {
            Assert.Contains("Yeni Hasta Kaydı</h2>", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Mükerrer Kontrolü Yap", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Kaydet ve Tamamla", component.Markup, StringComparison.Ordinal);
        });
    }

    private sealed class FakePatientHttpHandler : HttpMessageHandler
    {
        private readonly object? _responseBody;
        private readonly HttpStatusCode _statusCode;

        public FakePatientHttpHandler(object? responseBody = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (_statusCode == HttpStatusCode.Forbidden)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
            }

            if (request.RequestUri?.AbsolutePath.Contains("identity/session", StringComparison.Ordinal) == true)
            {
                var account = new CurrentAccountResponse(
                    "DEMO-reg@hospital.invalid",
                    "Staff",
                    Guid.NewGuid().ToString(),
                    false,
                    Roles: ["RegistrationStaff"]);

                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(account),
                };
                return Task.FromResult(resp);
            }

            if (request.RequestUri?.AbsolutePath.Contains("identity/antiforgery", StringComparison.Ordinal) == true)
            {
                var antiforgery = new AntiforgeryTokenResponse("fake-csrf-token");
                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(antiforgery),
                };
                return Task.FromResult(resp);
            }

            var response = new HttpResponseMessage(_statusCode);
            if (_statusCode == HttpStatusCode.OK && _responseBody is not null)
            {
                response.Content = JsonContent.Create(_responseBody);
            }

            return Task.FromResult(response);
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
