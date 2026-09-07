using System.Globalization;
using System.Net;
using System.Net.Http.Json;

using Bunit;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Web.Client.Identity;
using HospitalManagement.Web.Client.Pages.Admin;

using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class UserManagementComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F02-G08")]
    public void UserManagementRendersUserListTableWhenLoaded()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var sampleUsers = new UserListResponse(
            [
                new UserSummaryResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "DEMO-admin@hospital.invalid",
                    "Staff",
                    IsEnabled: true,
                    EmailConfirmed: true,
                    Roles: ["ADM"],
                    CreatedAtUtc: DateTime.UtcNow),
                new UserSummaryResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "DEMO-doctor@hospital.invalid",
                    "Staff",
                    IsEnabled: true,
                    EmailConfirmed: true,
                    Roles: ["DOC"],
                    CreatedAtUtc: DateTime.UtcNow),
            ],
            TotalCount: 2,
            Page: 1,
            PageSize: 50);

        var handler = new FakeHttpHandler(sampleUsers);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var apiClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(apiClient);

        context.Services.AddSingleton(apiClient);
        context.Services.AddSingleton(sessionState);

        var component = context.Render<UserManagement>();

        Assert.Contains("Kullanıcı ve Rol Yönetimi", component.Markup, StringComparison.Ordinal);
        Assert.Contains("DEMO-admin@hospital.invalid", component.Markup, StringComparison.Ordinal);
        Assert.Contains("DEMO-doctor@hospital.invalid", component.Markup, StringComparison.Ordinal);
        Assert.Contains("ADM", component.Markup, StringComparison.Ordinal);
        Assert.Contains("DOC", component.Markup, StringComparison.Ordinal);
        Assert.Contains("Devre Dışı Bırak", component.Markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F02-G08")]
    public void UserManagementRendersForbiddenStateWhenAccessDenied()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var handler = new FakeHttpHandler(statusCode: HttpStatusCode.Forbidden);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var apiClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(apiClient);

        context.Services.AddSingleton(apiClient);
        context.Services.AddSingleton(sessionState);

        var component = context.Render<UserManagement>();
        var nav = context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();

        component.WaitForAssertion(() =>
            Assert.Contains("account/login", nav.Uri, StringComparison.Ordinal));
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly object? _responseBody;
        private readonly HttpStatusCode _statusCode;

        public FakeHttpHandler(object? responseBody = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_statusCode == HttpStatusCode.Forbidden)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
            }

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.Contains("identity/session", StringComparison.OrdinalIgnoreCase))
            {
                var account = new CurrentAccountResponse(
                    "demo-admin@hospital.invalid",
                    "Staff",
                    Guid.NewGuid().ToString(),
                    false,
                    Roles: ["SystemAdministrator"]);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(account),
                });
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
