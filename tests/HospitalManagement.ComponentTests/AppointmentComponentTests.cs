using System.Globalization;
using System.Net;
using System.Net.Http.Json;

using Bunit;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Scheduling;
using HospitalManagement.Web.Client.Identity;
using HospitalManagement.Web.Client.Pages.Appointments;
using HospitalManagement.Web.Client.Scheduling;

using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class AppointmentComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F03-G05")]
    public void BookAppointmentPageRendersDoctorSelectionAndSlots()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var deptId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var slotTime = DateTime.UtcNow.Date.AddHours(10);

        var sampleSlots = new List<DoctorAvailabilityDayResponse>
        {
            new(today,
            [
                new AppointmentSlotResponse(
                    Guid.NewGuid(),
                    doctorId,
                    deptId,
                    slotTime,
                    slotTime.AddMinutes(20),
                    "Available",
                    null,
                    1),
            ]),
        };

        var handler = new FakeSchedulingHttpHandler(availabilityDays: sampleSlots);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var schedulingClient = new SchedulingApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<ISchedulingApiClient>(schedulingClient);

        var component = context.Render<BookAppointmentPage>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Randevu Arama ve Alma", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Kardiyoloji Polikliniği", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Prof. Dr. Ayşe Yılmaz", component.Markup, StringComparison.Ordinal);
            Assert.Contains(slotTime.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture), component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F03-G05")]
    public void MyAppointmentsPageRendersListAndOpensCancelModal()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var apptId = Guid.NewGuid();
        var sampleAppts = new List<AppointmentDetailResponse>
        {
            new(
                apptId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow.AddDays(1),
                "Confirmed",
                "Kalp Çarpıntısı",
                null,
                null,
                null,
                null,
                null,
                1),
        };

        var handler = new FakeSchedulingHttpHandler(appointments: sampleAppts);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var schedulingClient = new SchedulingApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<ISchedulingApiClient>(schedulingClient);

        var component = context.Render<MyAppointments>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Randevularım", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Kalp Çarpıntısı", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Onaylandı", component.Markup, StringComparison.Ordinal);
            Assert.Contains("İptal Et", component.Markup, StringComparison.Ordinal);
        });

        // Click "İptal Et"
        var cancelButton = component.Find("button.ui-button--danger");
        cancelButton.Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Randevu İptal Onayı</h2>", component.Markup, StringComparison.Ordinal);
            Assert.Contains("İptal Gerekçesi", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F03-G05")]
    public void MyAppointmentsRendersForbiddenStateWhenAccessDenied()
    {
        using var context = new BunitContext();
        context.Services.AddLocalization();
        using var cultureScope = new CultureScope("tr-TR");

        var handler = new FakeSchedulingHttpHandler(statusCode: HttpStatusCode.Forbidden);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var identityClient = new IdentityApiClient(httpClient);
        var sessionState = new UserSessionState(identityClient);
        var schedulingClient = new SchedulingApiClient(httpClient);

        context.Services.AddSingleton(identityClient);
        context.Services.AddSingleton(sessionState);
        context.Services.AddSingleton<ISchedulingApiClient>(schedulingClient);

        var component = context.Render<MyAppointments>();
        var nav = context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();

        component.WaitForAssertion(() =>
            Assert.Contains("account/login", nav.Uri, StringComparison.Ordinal));
    }

    private sealed class FakeSchedulingHttpHandler : HttpMessageHandler
    {
        private readonly List<DoctorAvailabilityDayResponse>? _availabilityDays;
        private readonly List<AppointmentDetailResponse>? _appointments;
        private readonly HttpStatusCode _statusCode;

        public FakeSchedulingHttpHandler(
            List<DoctorAvailabilityDayResponse>? availabilityDays = null,
            List<AppointmentDetailResponse>? appointments = null,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _availabilityDays = availabilityDays;
            _appointments = appointments;
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

            if (path.Contains("scheduling/availability", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_availabilityDays ?? []),
                };
            }

            if (path.Contains("scheduling/appointments/by-patient", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_appointments ?? []),
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
