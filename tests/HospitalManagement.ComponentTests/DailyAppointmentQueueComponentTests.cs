using System.Globalization;
using System.Net;
using System.Net.Http.Json;

using Bunit;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Scheduling;
using HospitalManagement.Web.Client.Identity;
using HospitalManagement.Web.Client.Pages.Staff;
using HospitalManagement.Web.Client.Scheduling;

using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class DailyAppointmentQueueComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F03-G06")]
    public void DailyAppointmentQueueRendersTableAndQueueNumbers()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var appt1Id = Guid.NewGuid();
        var appt2Id = Guid.NewGuid();
        var today = DateTime.UtcNow.Date;

        var sampleAppts = new List<AppointmentDetailResponse>
        {
            new(
                appt1Id,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                today.AddHours(9),
                "CheckedIn",
                "Göğüs Ağrısı",
                null,
                null,
                today.AddHours(8).AddMinutes(50),
                null,
                1,
                2),
            new(
                appt2Id,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                today.AddHours(9).AddMinutes(20),
                "Confirmed",
                "Tansiyon Takibi",
                null,
                null,
                null,
                null,
                null,
                1),
        };

        var handler = new FakeDailyQueueHttpHandler(dailyAppointments: sampleAppts);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var schedulingClient = new SchedulingApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<ISchedulingApiClient>(schedulingClient);

        var component = context.Render<DailyAppointmentQueue>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Günlük Randevu ve Sıra Yönetimi", component.Markup, StringComparison.Ordinal);
            Assert.Contains("#1", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Göğüs Ağrısı", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Tansiyon Takibi", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Giriş Yap", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Gelmedi", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F03-G06")]
    public void DailyAppointmentQueuePerformsCheckInAndDisplaysSuccess()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var apptId = Guid.NewGuid();
        var today = DateTime.UtcNow.Date;

        var sampleAppts = new List<AppointmentDetailResponse>
        {
            new(
                apptId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                today.AddHours(10),
                "Confirmed",
                "Genel Muayene",
                null,
                null,
                null,
                null,
                null,
                1),
        };

        var checkedInResult = new AppointmentDetailResponse(
            apptId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            today.AddHours(10),
            "CheckedIn",
            "Genel Muayene",
            null,
            null,
            today.AddHours(9).AddMinutes(55),
            null,
            2,
            2);

        var handler = new FakeDailyQueueHttpHandler(
            dailyAppointments: sampleAppts,
            checkInResponse: checkedInResult);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var schedulingClient = new SchedulingApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<ISchedulingApiClient>(schedulingClient);

        var component = context.Render<DailyAppointmentQueue>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Giriş Yap", component.Markup, StringComparison.Ordinal);
        });

        // Click "Giriş Yap"
        var checkInBtn = component.Find("button.ui-button--primary");
        checkInBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Hasta girişi yapıldı. Sıra Numarası: #2", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F03-G06")]
    public void DailyAppointmentQueueRendersForbiddenStateWhenAccessDenied()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var handler = new FakeDailyQueueHttpHandler(statusCode: HttpStatusCode.Forbidden);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var schedulingClient = new SchedulingApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<ISchedulingApiClient>(schedulingClient);

        var component = context.Render<DailyAppointmentQueue>();
        var nav = context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();

        component.WaitForAssertion(() =>
            Assert.Contains("account/login", nav.Uri, StringComparison.Ordinal));
    }

    private sealed class FakeDailyQueueHttpHandler : HttpMessageHandler
    {
        private readonly List<AppointmentDetailResponse>? _dailyAppointments;
        private readonly AppointmentDetailResponse? _checkInResponse;
        private readonly HttpStatusCode _statusCode;

        public FakeDailyQueueHttpHandler(
            List<AppointmentDetailResponse>? dailyAppointments = null,
            AppointmentDetailResponse? checkInResponse = null,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _dailyAppointments = dailyAppointments;
            _checkInResponse = checkInResponse;
            _statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (_statusCode == HttpStatusCode.Forbidden)
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.Contains("identity/session", StringComparison.OrdinalIgnoreCase))
            {
                var account = new CurrentAccountResponse(
                    "demo-receptionist@hospital.invalid",
                    "Staff",
                    Guid.NewGuid().ToString(),
                    false,
                    Roles: ["RegistrationStaff"]);
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

            if (path.Contains("scheduling/appointments/daily", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_dailyAppointments ?? []),
                };
            }

            if (path.Contains("check-in", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_checkInResponse ?? _dailyAppointments?.FirstOrDefault()),
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
