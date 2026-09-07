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
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Application;
using HospitalManagement.Modules.Scheduling.Domain;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class DoctorAvailabilityScheduleIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F03-G03")]
    public async Task DoctorScheduleCreationSlotGenerationAndLeaveBlockLifecycle()
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
        }

        // Seed demo identity
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();
        }

        using var client = CreateSecureClient(application);

        var docDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Doctor);
        var cmoDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.ChiefMedicalOfficer);

        // 1. Giriş yap (Başhekim/Yönetici takvim oluşturabilir)
        var loginResp = await LoginAsync(client, cmoDef.Email, cmoDef.Password);
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var doctorId = docDef.PersonId;
        var departmentId = Guid.Parse("30000000-0000-0000-0000-000000000003"); // Kardiyoloji

        var invalidDayResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest
            {
                DoctorId = doctorId,
                DepartmentId = departmentId,
                DayOfWeek = "Not-A-Day",
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(13, 0),
                SlotDurationMinutes = 30,
            });
        Assert.Equal(HttpStatusCode.BadRequest, invalidDayResponse.StatusCode);

        // 2. Doktor için Pazartesi çalışma takvimi oluştur (09:00 - 13:00, 30 dk slot, mola 11:00 - 11:30)
        var createScheduleRequest = new CreateDoctorScheduleRequest
        {
            DoctorId = doctorId,
            DepartmentId = departmentId,
            DayOfWeek = "Monday",
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(13, 0),
            SlotDurationMinutes = 30,
            Breaks =
            [
                new ScheduleBreakDto(new TimeOnly(11, 0), new TimeOnly(11, 30), "Mola"),
            ],
        };

        var scheduleResp = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/scheduling/schedules",
            createScheduleRequest);
        Assert.Equal(HttpStatusCode.Created, scheduleResp.StatusCode);
        var createdSchedule = await scheduleResp.Content.ReadFromJsonAsync<DoctorScheduleResponse>();
        Assert.NotNull(createdSchedule);
        Assert.Equal("Monday", createdSchedule.DayOfWeek);

        var invalidTimeZoneResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/scheduling/slots/generate",
            new GenerateSlotsRequest
            {
                DoctorId = doctorId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow),
                TimeZoneId = "Invalid/Time-Zone",
            });
        Assert.Equal(HttpStatusCode.BadRequest, invalidTimeZoneResponse.StatusCode);

        // 3. Aynı gün için tekrar takvim oluşturmayı deneme -> 409 Conflict
        var conflictScheduleResp = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/scheduling/schedules",
            createScheduleRequest);
        Assert.Equal(HttpStatusCode.Conflict, conflictScheduleResp.StatusCode);

        // 4. Gelecek Pazartesi için slot üret
        var nextMonday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        while (nextMonday.DayOfWeek != DayOfWeek.Monday)
        {
            nextMonday = nextMonday.AddDays(1);
        }

        var genSlotsResp = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/scheduling/slots/generate",
            new GenerateSlotsRequest
            {
                DoctorId = doctorId,
                StartDate = nextMonday,
                EndDate = nextMonday,
                TimeZoneId = "UTC",
            });
        Assert.Equal(HttpStatusCode.OK, genSlotsResp.StatusCode);
        var generatedCount = await genSlotsResp.Content.ReadFromJsonAsync<int>();
        // 09:00 - 13:00 is 4 hours = 8 slots. 11:00-11:30 break removes 1 slot = 7 slots.
        Assert.Equal(7, generatedCount);

        // 5. Uygunluk sorgulama
        var availResp = await client.GetAsync(
            $"/api/v1/scheduling/availability/by-doctor/{doctorId}?startDate={nextMonday:O}&endDate={nextMonday:O}&timeZoneId=UTC");
        Assert.Equal(HttpStatusCode.OK, availResp.StatusCode);
        var availDays = await availResp.Content.ReadFromJsonAsync<IReadOnlyList<DoctorAvailabilityDayResponse>>();
        Assert.NotNull(availDays);
        Assert.Single(availDays);
        Assert.Equal(7, availDays[0].Slots.Count);
        Assert.All(availDays[0].Slots, s => Assert.Equal("Available", s.Status));

        // 6. Doktor İzin/Blokaj oluştur (Pazartesi 09:00 - 10:00 UTC) -> İlk 2 slot Blocked olmalıdır
        var leaveStartUtc = nextMonday.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc);
        var leaveEndUtc = nextMonday.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);

        var leaveResp = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/scheduling/leave-blocks",
            new CreateDoctorLeaveBlockRequest
            {
                DoctorId = doctorId,
                StartUtc = leaveStartUtc,
                EndUtc = leaveEndUtc,
                Reason = "Kongre Katılımı",
            });
        Assert.Equal(HttpStatusCode.Created, leaveResp.StatusCode);

        // Tekrar uygunluk sorgula -> hasta/istemci sözleşmesi yalnız 5 rezerve edilebilir slotu döndürmelidir.
        var availAfterLeaveResp = await client.GetAsync(
            $"/api/v1/scheduling/availability/by-doctor/{doctorId}?startDate={nextMonday:O}&endDate={nextMonday:O}&timeZoneId=UTC");
        Assert.Equal(HttpStatusCode.OK, availAfterLeaveResp.StatusCode);
        var availAfterLeaveDays = await availAfterLeaveResp.Content.ReadFromJsonAsync<IReadOnlyList<DoctorAvailabilityDayResponse>>();
        Assert.NotNull(availAfterLeaveDays);
        Assert.Single(availAfterLeaveDays);
        var slots = availAfterLeaveDays[0].Slots;
        Assert.Equal(5, slots.Count);
        Assert.All(slots, slot =>
        {
            Assert.Equal("Available", slot.Status);
            Assert.Null(slot.HeldByPersonId);
        });
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
