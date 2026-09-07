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

public sealed class DailyAppointmentQueueIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F03-G06")]
    public async Task StaffCanListDailyAppointmentsCheckInWithSequentialQueueNumbersAndMarkNoShow()
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

        // Seed only demo identity; this test creates isolated queue slots explicitly.
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();
        }

        var regDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.RegistrationStaff);
        var patientDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Patient);
        var docDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Doctor);

        using var staffClient = CreateSecureClient(application);
        var loginResp = await LoginAsync(staffClient, regDef.Email, regDef.Password);
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        using var secondStaffClient = CreateSecureClient(application);
        var secondLoginResp = await LoginAsync(secondStaffClient, regDef.Email, regDef.Password);
        Assert.Equal(HttpStatusCode.OK, secondLoginResp.StatusCode);

        // Create 3 appointment slots for today directly
        var today = DateTime.UtcNow.Date;
        var slot1Id = Guid.NewGuid();
        var slot2Id = Guid.NewGuid();
        var slot3Id = Guid.NewGuid();

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            var deptId = Guid.Parse("30000000-0000-0000-0000-000000000003");

            var slot1 = AppointmentSlot.Create(slot1Id, docDef.PersonId, deptId, null, today.AddHours(9), today.AddHours(9).AddMinutes(20), DateTime.UtcNow);
            var slot2 = AppointmentSlot.Create(slot2Id, docDef.PersonId, deptId, null, today.AddHours(9).AddMinutes(20), today.AddHours(9).AddMinutes(40), DateTime.UtcNow);
            var slot3 = AppointmentSlot.Create(slot3Id, docDef.PersonId, deptId, null, today.AddHours(9).AddMinutes(40), today.AddHours(10), DateTime.UtcNow);

            schedulingDb.AppointmentSlots.AddRange(slot1, slot2, slot3);
            await schedulingDb.SaveChangesAsync();
        }

        // Book all 3 appointments
        var book1 = await PostWithAntiforgeryAsync(staffClient, "/api/v1/scheduling/appointments/book", new BookAppointmentRequest
        {
            SlotId = slot1Id,
            PatientId = Guid.NewGuid(),
            ReasonForVisit = "Hasta 1 Kontrol",
        });
        Assert.Equal(HttpStatusCode.Created, book1.StatusCode);
        var appt1 = await book1.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(appt1);

        var book2 = await PostWithAntiforgeryAsync(staffClient, "/api/v1/scheduling/appointments/book", new BookAppointmentRequest
        {
            SlotId = slot2Id,
            PatientId = Guid.NewGuid(),
            ReasonForVisit = "Hasta 2 Kontrol",
        });
        Assert.Equal(HttpStatusCode.Created, book2.StatusCode);
        var appt2 = await book2.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(appt2);

        var book3 = await PostWithAntiforgeryAsync(staffClient, "/api/v1/scheduling/appointments/book", new BookAppointmentRequest
        {
            SlotId = slot3Id,
            PatientId = Guid.NewGuid(),
            ReasonForVisit = "Hasta 3 Kontrol",
        });
        Assert.Equal(HttpStatusCode.Created, book3.StatusCode);
        var appt3 = await book3.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(appt3);

        // 1. Günlük Randevu Listesini Çek
        var dailyResp = await staffClient.GetAsync($"/api/v1/scheduling/appointments/daily?date={DateOnly.FromDateTime(today):O}&doctorId={docDef.PersonId}");
        Assert.Equal(HttpStatusCode.OK, dailyResp.StatusCode);
        var dailyList = await dailyResp.Content.ReadFromJsonAsync<IReadOnlyList<AppointmentDetailResponse>>();
        Assert.NotNull(dailyList);
        Assert.True(dailyList.Count >= 3);

        // 2-3. İki ayrı kayıt görevlisi eşzamanlı check-in yapar; sıra numaraları tekil ve ardışık kalmalıdır.
        var checkIn1Task = PostWithAntiforgeryAsync(
            staffClient,
            $"/api/v1/scheduling/appointments/{appt1.Id}/check-in",
            new
            {
            });
        var checkIn2Task = PostWithAntiforgeryAsync(
            secondStaffClient,
            $"/api/v1/scheduling/appointments/{appt2.Id}/check-in",
            new
            {
            });
        var checkInResponses = await Task.WhenAll(checkIn1Task, checkIn2Task);
        var checkIn1Resp = checkInResponses[0];
        var checkIn2Resp = checkInResponses[1];
        Assert.Equal(HttpStatusCode.OK, checkIn1Resp.StatusCode);
        Assert.Equal(HttpStatusCode.OK, checkIn2Resp.StatusCode);

        var checkedIn1 = await checkIn1Resp.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        var checkedIn2 = await checkIn2Resp.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(checkedIn1);
        Assert.NotNull(checkedIn2);
        Assert.Equal("CheckedIn", checkedIn1.Status);
        Assert.Equal("CheckedIn", checkedIn2.Status);
        Assert.Equal(
            new int?[] { 1, 2 },
            new[] { checkedIn1.QueueNumber, checkedIn2.QueueNumber }.Order().ToArray());

        // 4. Tekrar check-in denemesi (Geçersiz durum geçişi) -> 409 Conflict
        var doubleCheckInResp = await PostWithAntiforgeryAsync(staffClient, $"/api/v1/scheduling/appointments/{appt1.Id}/check-in", new
        {
        });
        Assert.Equal(HttpStatusCode.Conflict, doubleCheckInResp.StatusCode);

        // 5. Üçüncü hastayı gelmedi (No-Show) olarak işaretle
        var noShowResp = await PostWithAntiforgeryAsync(staffClient, $"/api/v1/scheduling/appointments/{appt3.Id}/no-show", new
        {
        });
        Assert.Equal(HttpStatusCode.OK, noShowResp.StatusCode);
        var noShowAppt = await noShowResp.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(noShowAppt);
        Assert.Equal("NoShow", noShowAppt.Status);

        // 6. No-show olan randevuyu check-in yapmaya çalışma -> 409 Conflict
        var invalidCheckInResp = await PostWithAntiforgeryAsync(staffClient, $"/api/v1/scheduling/appointments/{appt3.Id}/check-in", new
        {
        });
        Assert.Equal(HttpStatusCode.Conflict, invalidCheckInResp.StatusCode);

        // 7. Yetkisiz erişim testi (Hasta rolü günlük kuyruğu göremez)
        using var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, patientDef.Email, patientDef.Password);
        var forbiddenResp = await patientClient.GetAsync($"/api/v1/scheduling/appointments/daily?date={DateOnly.FromDateTime(today):O}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResp.StatusCode);

        // 8. Denetim İzi Doğrulama
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var auditLogs = await auditDb.AuditLogs
                .Where(l => l.TargetResourceType == "Scheduling")
                .ToListAsync();

            Assert.Contains(auditLogs, l => l.Action == "Appointment.CheckIn" && l.TargetResourceId == appt1.Id.ToString());
            Assert.Contains(auditLogs, l => l.Action == "Appointment.CheckIn" && l.TargetResourceId == appt2.Id.ToString());
            Assert.Contains(auditLogs, l => l.Action == "Appointment.NoShow" && l.TargetResourceId == appt3.Id.ToString());
            Assert.Contains(auditLogs, l => l.Action == "Appointment.DailyList");
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
