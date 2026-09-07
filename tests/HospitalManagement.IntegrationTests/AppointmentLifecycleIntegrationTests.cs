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

public sealed class AppointmentLifecycleIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F03-G04")]
    public async Task AtomicSlotBookingPreventsDoubleBookingConcurrently()
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

        // Seed demo identity
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();
        }

        var docDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Doctor);
        var regDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.RegistrationStaff);

        // Create a slot in DB directly
        var slotId = Guid.NewGuid();
        var patient1Id = Guid.NewGuid();
        var patient2Id = Guid.NewGuid();

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            var slot = AppointmentSlot.Create(
                slotId,
                docDef.PersonId,
                Guid.Parse("30000000-0000-0000-0000-000000000003"),
                null,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(2).AddMinutes(20),
                DateTime.UtcNow);
            schedulingDb.AppointmentSlots.Add(slot);
            await schedulingDb.SaveChangesAsync();
        }

        using var client1 = CreateSecureClient(application);
        using var client2 = CreateSecureClient(application);

        await LoginAsync(client1, regDef.Email, regDef.Password);
        await LoginAsync(client2, regDef.Email, regDef.Password);

        // Concurrently attempt to book the exact same slot for two different patients
        var task1 = PostWithAntiforgeryAsync(
            client1,
            "/api/v1/scheduling/appointments/book",
            new BookAppointmentRequest
            {
                SlotId = slotId,
                PatientId = patient1Id,
                ReasonForVisit = "Hasta 1 Kontrol",
            });

        var task2 = PostWithAntiforgeryAsync(
            client2,
            "/api/v1/scheduling/appointments/book",
            new BookAppointmentRequest
            {
                SlotId = slotId,
                PatientId = patient2Id,
                ReasonForVisit = "Hasta 2 Kontrol",
            });

        var responses = await Task.WhenAll(task1, task2);

        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F03-G04")]
    public async Task FullAppointmentLifecycleAndCancellationReopeningSlotWorks()
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

        // Seed demo identity
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();
        }

        using var client = CreateSecureClient(application);
        var regDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.RegistrationStaff);
        await LoginAsync(client, regDef.Email, regDef.Password);

        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            var slot = AppointmentSlot.Create(
                slotId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                DateTime.UtcNow.AddDays(3),
                DateTime.UtcNow.AddDays(3).AddMinutes(15),
                DateTime.UtcNow);
            schedulingDb.AppointmentSlots.Add(slot);
            await schedulingDb.SaveChangesAsync();
        }

        // 1. Randevu Al (Book)
        var bookResp = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/scheduling/appointments/book",
            new BookAppointmentRequest
            {
                SlotId = slotId,
                PatientId = patientId,
                ReasonForVisit = "Periyodik Muayene",
            });
        Assert.Equal(HttpStatusCode.Created, bookResp.StatusCode);
        var bookedAppt = await bookResp.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(bookedAppt);
        Assert.Equal("Confirmed", bookedAppt.Status);

        // 2. Randevu İptal Et (Cancel)
        var cancelResp = await PostWithAntiforgeryAsync(
            client,
            $"/api/v1/scheduling/appointments/{bookedAppt.Id}/cancel",
            new CancelAppointmentRequest { Reason = "Hasta gelemeyecek" });
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);
        var cancelledAppt = await cancelResp.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(cancelledAppt);
        Assert.Equal("Cancelled", cancelledAppt.Status);

        // 3. Slot artık serbest (Available) olmalı ve tekrar rezerve edilebilmelidir
        var reBookResp = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/scheduling/appointments/book",
            new BookAppointmentRequest
            {
                SlotId = slotId,
                PatientId = Guid.NewGuid(),
                ReasonForVisit = "Yeni Hasta Muayenesi",
            });
        Assert.Equal(HttpStatusCode.Created, reBookResp.StatusCode);
        var reBookedAppt = await reBookResp.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(reBookedAppt);
        Assert.Equal("Confirmed", reBookedAppt.Status);

        // 4. Check-in Yap
        var checkInResp = await PostWithAntiforgeryAsync(
            client,
            $"/api/v1/scheduling/appointments/{reBookedAppt.Id}/check-in",
            new
            {
            });
        Assert.Equal(HttpStatusCode.OK, checkInResp.StatusCode);
        var checkedInAppt = await checkInResp.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(checkedInAppt);
        Assert.Equal("CheckedIn", checkedInAppt.Status);
        Assert.NotNull(checkedInAppt.CheckedInAtUtc);
        Assert.Equal(1, checkedInAppt.QueueNumber);

        // 5. Muayeneyi Tamamla (Complete)
        var completeResp = await PostWithAntiforgeryAsync(
            client,
            $"/api/v1/scheduling/appointments/{reBookedAppt.Id}/complete",
            new
            {
            });
        Assert.Equal(HttpStatusCode.OK, completeResp.StatusCode);
        var completedAppt = await completeResp.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(completedAppt);
        Assert.Equal("Completed", completedAppt.Status);
        Assert.NotNull(completedAppt.CompletedAtUtc);
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
