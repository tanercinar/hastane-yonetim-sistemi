using System.Net;
using System.Net.Http.Json;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Scheduling;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Application;
using HospitalManagement.Modules.Scheduling.Domain;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class PatientAppointmentSearchAndBookingIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F03-G05")]
    public async Task PatientSearchesAvailableSlotsBooksAndCancelsAppointmentWithAudit()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        // Run migrations
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identityDb = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
            await identityDb.Database.MigrateAsync();
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            await auditDb.Database.MigrateAsync();
            var patientsDb = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();
            await patientsDb.Database.MigrateAsync();
            var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            await schedulingDb.Database.MigrateAsync();
            var notificationsDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            await notificationsDb.Database.MigrateAsync();
        }

        // Seed demo identity & scheduling
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();
            var schedulingSeeder = scope.ServiceProvider.GetRequiredService<ISchedulingDataSeeder>();
            await schedulingSeeder.SeedAsync();
        }

        using var client = CreateSecureClient(application);
        var patientDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Patient);
        var docDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Doctor);

        // 1. Hasta Girişi
        var loginResp = await LoginAsync(client, patientDef.Email, patientDef.Password);
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        // 2. Doktor uygunluk sorgulama
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var availResp = await client.GetAsync(
            $"/api/v1/scheduling/availability/by-doctor/{docDef.PersonId}?startDate={today:O}&endDate={today.AddDays(7):O}");
        Assert.Equal(HttpStatusCode.OK, availResp.StatusCode);
        var days = await availResp.Content.ReadFromJsonAsync<IReadOnlyList<DoctorAvailabilityDayResponse>>();
        Assert.NotNull(days);
        Assert.NotEmpty(days);

        var firstDayWithSlots = days.FirstOrDefault(d => d.Slots.Any(s => s.Status == "Available"));
        Assert.NotNull(firstDayWithSlots);
        var availableSlot = firstDayWithSlots.Slots.First(s => s.Status == "Available");

        // 3. Randevu Alma (Book)
        var bookResp = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/scheduling/appointments/book",
            new BookAppointmentRequest
            {
                SlotId = availableSlot.Id,
                PatientId = patientDef.PersonId,
                ReasonForVisit = "Genel Kardiyoloji Kontrolü",
            });
        Assert.Equal(HttpStatusCode.Created, bookResp.StatusCode);
        var bookedAppt = await bookResp.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(bookedAppt);
        Assert.Equal("Confirmed", bookedAppt.Status);
        Assert.Equal("Genel Kardiyoloji Kontrolü", bookedAppt.ReasonForVisit);

        // 4. Kendi Randevularını Listeleme
        var myApptsResp = await client.GetAsync($"/api/v1/scheduling/appointments/by-patient/{patientDef.PersonId}");
        Assert.Equal(HttpStatusCode.OK, myApptsResp.StatusCode);
        var myAppts = await myApptsResp.Content.ReadFromJsonAsync<IReadOnlyList<AppointmentDetailResponse>>();
        Assert.NotNull(myAppts);
        Assert.Contains(myAppts, a => a.Id == bookedAppt.Id && a.Status == "Confirmed");

        // 5. Randevu İptal Etme
        var cancelResp = await PostWithAntiforgeryAsync(
            client,
            $"/api/v1/scheduling/appointments/{bookedAppt.Id}/cancel",
            new CancelAppointmentRequest { Reason = "İş seyahati nedeniyle gelemeyeceğim" });
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);
        var cancelledAppt = await cancelResp.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(cancelledAppt);
        Assert.Equal("Cancelled", cancelledAppt.Status);

        // 6. Denetim İzi Doğrulama
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var auditLogs = await auditDb.AuditLogs
                    .Where(l => l.TargetResourceType == "Scheduling" && l.TargetResourceId == bookedAppt.Id.ToString())
                    .ToListAsync();

            Assert.Contains(auditLogs, l => l.Action == "Appointment.Book");
            Assert.Contains(auditLogs, l => l.Action == "Appointment.Cancel");
            Assert.DoesNotContain(auditLogs, l =>
                l.Reason?.Contains("İş seyahati nedeniyle gelemeyeceğim", StringComparison.Ordinal) == true);
        }
    }

    private static HttpClient CreateSecureClient(ApiWebApplicationFactory application) =>
        application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost", UriKind.Absolute),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    private static Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email,
        string password) =>
        PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/sessions",
            new LoginRequest { Email = email, Password = password });

    private static async Task<HttpResponseMessage> PostWithAntiforgeryAsync<TRequest>(
        HttpClient client,
        string requestUri,
        TRequest body)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/v1/identity/antiforgery");
        Assert.NotNull(token);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        request.Headers.Add("X-HMS-CSRF", token.Token);
        return await client.SendAsync(request);
    }
}
