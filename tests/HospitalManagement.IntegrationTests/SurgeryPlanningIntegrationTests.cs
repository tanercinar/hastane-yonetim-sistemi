using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Surgery;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;
using HospitalManagement.Modules.Emergency.Application;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class SurgeryPlanningIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-G04")]
    public async Task SurgeryBookingFlowRoomAndSurgeonCollisionAtomicRejectionAndPreOpClearanceSucceeds()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000121");
        var doctorPersonId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var anesthesiologistId = Guid.Parse("00000000-0000-0000-0000-000000000111");
        var otherSurgeonId = anesthesiologistId;

        var doctorClient = CreateSecureClient(application);
        var adminClient = CreateSecureClient(application);

        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var adminLogin = await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1");
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        // 1. Get Operating Rooms
        var rooms = await doctorClient.GetFromJsonAsync<List<OperatingRoomResponse>>("/api/v1/surgery/operating-rooms");
        Assert.NotNull(rooms);
        Assert.True(rooms.Count >= 3);
        var room1 = rooms.First(r => r.RoomCode == "DEMO-OR-01");
        var room2 = rooms.First(r => r.RoomCode == "DEMO-OR-02");

        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        var start1 = tomorrow.AddHours(9);
        var end1 = tomorrow.AddHours(11);

        // 2. Doctor books OR-01 from 09:00 to 11:00
        var book1Req = new CreateSurgeryBookingRequest(
            patientId: patientId,
            encounterId: null,
            departmentId: Guid.Parse("30000000-0000-0000-0000-000000000009"),
            departmentName: "Genel Cerrahi Anabilim Dalı",
            procedureName: "Laparoskopik Kolesistektomi",
            procedureCode: "DEMO-PRC-CHOLE",
            urgency: "Elective",
            operatingRoomId: room1.Id,
            leadSurgeonDoctorId: doctorPersonId,
            anesthesiologistDoctorId: anesthesiologistId,
            operatingNurseStaffId: null,
            scheduledStartTimeUtc: start1,
            scheduledEndTimeUtc: end1,
            clinicalNotes: "Rutin elektif cerrahi");

        var book1Resp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/surgery/bookings", book1Req);
        Assert.Equal(HttpStatusCode.Created, book1Resp.StatusCode);
        var booking1 = await book1Resp.Content.ReadFromJsonAsync<SurgeryBookingResponse>();
        Assert.NotNull(booking1);
        Assert.Equal("Scheduled", booking1.Status);
        Assert.StartsWith("DEMO-SURG-", booking1.BookingProtocolNumber, StringComparison.Ordinal);

        // 3. Collision Test 1: Room Conflict (Same OR-01, 10:00 to 12:00, different surgeon) -> 409 Conflict
        var roomConflictReq = new CreateSurgeryBookingRequest(
            patientId: patientId,
            encounterId: null,
            departmentId: Guid.Parse("30000000-0000-0000-0000-000000000009"),
            departmentName: "Genel Cerrahi Anabilim Dalı",
            procedureName: "Apendektomi",
            procedureCode: "DEMO-PRC-APP",
            urgency: "Expedited",
            operatingRoomId: room1.Id,
            leadSurgeonDoctorId: otherSurgeonId,
            anesthesiologistDoctorId: doctorPersonId,
            operatingNurseStaffId: null,
            scheduledStartTimeUtc: tomorrow.AddHours(10),
            scheduledEndTimeUtc: tomorrow.AddHours(12),
            clinicalNotes: "Çakışan oda denemesi");

        var roomConflictResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/surgery/bookings", roomConflictReq);
        Assert.Equal(HttpStatusCode.Conflict, roomConflictResp.StatusCode);

        // 4. Collision Test 2: Surgeon Conflict (Different OR-02, same surgeon, 10:30 to 12:30) -> 409 Conflict
        var surgeonConflictReq = new CreateSurgeryBookingRequest(
            patientId: patientId,
            encounterId: null,
            departmentId: Guid.Parse("30000000-0000-0000-0000-000000000009"),
            departmentName: "Genel Cerrahi Anabilim Dalı",
            procedureName: "Mide Rezeksiyonu",
            procedureCode: "DEMO-PRC-GASTR",
            urgency: "Elective",
            operatingRoomId: room2.Id,
            leadSurgeonDoctorId: doctorPersonId, // Same surgeon
            anesthesiologistDoctorId: anesthesiologistId,
            operatingNurseStaffId: null,
            scheduledStartTimeUtc: tomorrow.AddHours(10).AddMinutes(30),
            scheduledEndTimeUtc: tomorrow.AddHours(12).AddMinutes(30),
            clinicalNotes: "Çakışan cerrah denemesi");

        var surgeonConflictResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/surgery/bookings", surgeonConflictReq);
        Assert.Equal(HttpStatusCode.Conflict, surgeonConflictResp.StatusCode);

        // 5. Pre-Op Checklist Recording (6/6 cleared) -> Status becomes PreOpCleared
        var checklistReq = new RecordPreOpChecklistRequest(
            consentSigned: true,
            anesthesiaClearance: true,
            npoConfirmed: true,
            bloodProductsReserved: true,
            siteMarked: true,
            allergyChecked: true,
            notes: "Tüm pre-op kontroller ve anestezi viziti tamam.");

        var preOpResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/surgery/bookings/{booking1.Id}/pre-op-checklist",
            checklistReq);

        Assert.Equal(HttpStatusCode.OK, preOpResp.StatusCode);
        var clearedBooking = await preOpResp.Content.ReadFromJsonAsync<SurgeryBookingResponse>();
        Assert.NotNull(clearedBooking);
        Assert.Equal("PreOpCleared", clearedBooking.Status);
        Assert.NotNull(clearedBooking.PreOpChecklist);
        Assert.True(clearedBooking.PreOpChecklist.IsFullyCleared);

        // 6. Non-clinical Admin attempt -> 403 Forbidden
        var adminBookResp = await PostWithAntiforgeryAsync(adminClient, "/api/v1/surgery/bookings", book1Req);
        Assert.Equal(HttpStatusCode.Forbidden, adminBookResp.StatusCode);
    }

    private static HttpClient CreateSecureClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost/"),
        });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
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

    private static async Task RunAllMigrationsAndSeedAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var idDb = sp.GetRequiredService<IdentityAccessDbContext>();
        await idDb.Database.MigrateAsync();

        var auditDb = sp.GetRequiredService<AuditPrivacyDbContext>();
        await auditDb.Database.MigrateAsync();

        var patDb = sp.GetRequiredService<PatientsDbContext>();
        await patDb.Database.MigrateAsync();

        var orgDb = sp.GetRequiredService<OrganizationDbContext>();
        await orgDb.Database.MigrateAsync();

        var clinDb = sp.GetRequiredService<ClinicalRecordsDbContext>();
        await clinDb.Database.MigrateAsync();

        var diagDb = sp.GetRequiredService<DiagnosticsDbContext>();
        await diagDb.Database.MigrateAsync();

        var rxDb = sp.GetRequiredService<PharmacyDbContext>();
        await rxDb.Database.MigrateAsync();

        var schedDb = sp.GetRequiredService<SchedulingDbContext>();
        await schedDb.Database.MigrateAsync();

        var inpDb = sp.GetRequiredService<InpatientDbContext>();
        await inpDb.Database.MigrateAsync();

        var notifDb = sp.GetRequiredService<NotificationsDbContext>();
        await notifDb.Database.MigrateAsync();

        var emgDb = sp.GetRequiredService<EmergencyDbContext>();
        await emgDb.Database.MigrateAsync();

        var surgDb = sp.GetRequiredService<SurgeryDbContext>();
        await surgDb.Database.MigrateAsync();

        var idSeeder = sp.GetRequiredService<IIdentityDataSeeder>();
        await idSeeder.SeedAsync();

        var orgSeeder = sp.GetRequiredService<IOrganizationDataSeeder>();
        await orgSeeder.SeedAsync();

        var inpSeeder = sp.GetRequiredService<IInpatientDataSeeder>();
        await inpSeeder.SeedAsync();

        var emgSeeder = sp.GetRequiredService<IEmergencyDataSeeder>();
        await emgSeeder.SeedAsync();

        var surgSeeder = sp.GetRequiredService<ISurgeryDataSeeder>();
        await surgSeeder.SeedAsync();
    }
}
