using System.Net;
using System.Net.Http.Json;

using Bunit;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Web.Client.Identity;
using HospitalManagement.Web.Client.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class IdentityLoginComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F02-G05")]
    public void LoginRedirectsToTwoFactorChallengeWhenApiRequiresMfa()
    {
        using var context = CreateContext(new LoginHttpHandler(requiresTwoFactor: true));

        var component = context.Render<Login>();
        component.Find("#login-email").Change("DEMO-doctor@hospital.invalid");
        component.Find("#login-password").Change("DEMO-Doc-Pass!1");
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
            Assert.EndsWith(
                "/account/two-factor",
                context.Services.GetRequiredService<NavigationManager>().Uri,
                StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F03-KAPI")]
    public void LoginShowsRecoverableMessageWhenAntiforgeryEndpointIsUnavailable()
    {
        using var context = CreateContext(new LoginHttpHandler(antiforgeryStatus: HttpStatusCode.InternalServerError));

        var component = context.Render<Login>();
        component.Find("#login-email").Change("DEMO-patient@hospital.invalid");
        component.Find("#login-password").Change("DEMO-Patient-Pass!1");
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            var alert = component.Find("[role='alert']");
            Assert.Contains("Yerel altyapının ve HTTPS adresinin", alert.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("DEMO-Patient-Pass!1", component.Markup, StringComparison.Ordinal);
        });
    }

    private static BunitContext CreateContext(HttpMessageHandler handler)
    {
        var context = new BunitContext();
        context.Services.AddLocalization();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost", UriKind.Absolute),
        };
        var identityClient = new IdentityApiClient(httpClient);
        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(new UserSessionState(identityClient));
        return context;
    }

    private sealed class LoginHttpHandler(
        bool requiresTwoFactor = false,
        HttpStatusCode antiforgeryStatus = HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.EndsWith("identity/session", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            if (request.Method == HttpMethod.Get
                && request.RequestUri?.AbsolutePath.EndsWith("/identity/antiforgery", StringComparison.Ordinal) == true)
            {
                return Task.FromResult(new HttpResponseMessage(antiforgeryStatus)
                {
                    Content = JsonContent.Create(new AntiforgeryTokenResponse("DEMO-csrf-token")),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new LoginResponse("Oturum açıldı.", requiresTwoFactor)),
            });
        }
    }
}
