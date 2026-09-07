using System.Globalization;
using System.Net;
using System.Net.Http.Json;

using Bunit;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Notifications;
using HospitalManagement.Web.Client.Identity;
using HospitalManagement.Web.Client.Notifications;
using HospitalManagement.Web.Client.Pages.Notifications;

using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class NotificationComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F03-G07")]
    public void NotificationListRendersItemsAndUnreadCount()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var sampleNotifications = new List<NotificationDetailResponse>
        {
            new(
                Guid.NewGuid(),
                "Randevunuz Onaylandı",
                "28.08.2026 10:00 tarihindeki muayene randevunuz onaylanmıştır.",
                "/patient/appointments",
                false,
                DateTime.UtcNow.AddMinutes(-30),
                null),
            new(
                Guid.NewGuid(),
                "Randevu Girişiniz Yapıldı",
                "Muayene Sıra Numaranız: #1",
                "/patient/appointments",
                true,
                DateTime.UtcNow.AddHours(-2),
                DateTime.UtcNow.AddHours(-1)),
        };

        var handler = new FakeNotificationHttpHandler(notifications: sampleNotifications);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var notificationClient = new NotificationApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<INotificationApiClient>(notificationClient);

        var component = context.Render<NotificationList>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Bildirimlerim", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Randevunuz Onaylandı", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Muayene Sıra Numaranız: #1", component.Markup, StringComparison.Ordinal);
            Assert.Contains("okunmamış", component.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Okundu İşaretle", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F03-G07")]
    public void NotificationListPerformsMarkAsRead()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var notifId = Guid.NewGuid();
        var sampleNotifications = new List<NotificationDetailResponse>
        {
            new(
                notifId,
                "Yeni Bildirim",
                "Test bildirim mesajı.",
                null,
                false,
                DateTime.UtcNow,
                null),
        };

        var handler = new FakeNotificationHttpHandler(notifications: sampleNotifications);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var notificationClient = new NotificationApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<INotificationApiClient>(notificationClient);

        var component = context.Render<NotificationList>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Okundu İşaretle", component.Markup, StringComparison.Ordinal);
        });

        var markReadBtn = component.Find("button.notification-mark-read-btn");
        markReadBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.True(handler.MarkReadCalled);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F03-G07")]
    public void NotificationListRendersForbiddenStateWhenAccessDenied()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var handler = new FakeNotificationHttpHandler(statusCode: HttpStatusCode.Unauthorized);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var notificationClient = new NotificationApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<INotificationApiClient>(notificationClient);

        var component = context.Render<NotificationList>();
        var nav = context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("account/login", nav.Uri, StringComparison.Ordinal);
        });
    }

    private sealed class FakeNotificationHttpHandler : HttpMessageHandler
    {
        private readonly List<NotificationDetailResponse>? _notifications;
        private readonly HttpStatusCode _statusCode;

        public bool MarkReadCalled
        {
            get; private set;
        }

        public FakeNotificationHttpHandler(
            List<NotificationDetailResponse>? notifications = null,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _notifications = notifications;
            _statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (_statusCode != HttpStatusCode.OK)
            {
                return new HttpResponseMessage(_statusCode);
            }

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.Contains("identity/session", StringComparison.OrdinalIgnoreCase))
            {
                var account = new CurrentAccountResponse(
                    "demo-patient@hospital.invalid",
                    "Patient",
                    Guid.NewGuid().ToString(),
                    false,
                    Roles: ["Patient"]);
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

            if (path.Contains("notifications/my", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_notifications ?? []),
                };
            }

            if (path.Contains("/read", StringComparison.OrdinalIgnoreCase))
            {
                MarkReadCalled = true;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(true),
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
