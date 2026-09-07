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

public sealed class PerioperativeRecordIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-G05")]
    public async Task PerioperativeRecordFullFlowDraftSaveSignImmutabilityAndCorrectionSucceeds()
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

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // 1. Get Operating Room
        var rooms = await doctorClient.GetFromJsonAsync<List<OperatingRoomResponse>>("/api/v1/surgery/operating-rooms");
        Assert.NotNull(rooms);
        var room = rooms.First();

        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        var start = tomorrow.AddHours(9);
        var end = tomorrow.AddHours(12);

        // 2. Create Surgery Booking
        var bookingReq = new CreateSurgeryBookingRequest(
            patientId: patientId,
            encounterId: null,
            departmentId: Guid.Parse("30000000-0000-0000-0000-000000000009"),
            departmentName: "Genel Cerrahi Anabilim Dalı",
            procedureName: "Laparoskopik Kolesistektomi",
            procedureCode: "DEMO-PRC-CHOLE",
            urgency: "Elective",
            operatingRoomId: room.Id,
            leadSurgeonDoctorId: doctorPersonId,
            anesthesiologistDoctorId: anesthesiologistId,
            operatingNurseStaffId: null,
            scheduledStartTimeUtc: start,
            scheduledEndTimeUtc: end,
            clinicalNotes: "Rutin elektif kolesistektomi");

        var bookResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/surgery/bookings", bookingReq);
        Assert.Equal(HttpStatusCode.Created, bookResp.StatusCode);
        var booking = await bookResp.Content.ReadFromJsonAsync<SurgeryBookingResponse>();
        Assert.NotNull(booking);

        // 3. Save Perioperative Record Draft
        var t1 = start.AddMinutes(5);
        var t2 = start.AddMinutes(15);
        var t3 = start.AddMinutes(30);
        var t4 = start.AddMinutes(90);
        var t5 = start.AddMinutes(105);
        var t6 = start.AddMinutes(115);

        var saveReq = new SavePerioperativeRecordRequest(
            surgeryBookingId: booking.Id,
            roomEntryTimeUtc: t1,
            anesthesiaStartTimeUtc: t2,
            incisionTimeUtc: t3,
            closureTimeUtc: t4,
            anesthesiaEndTimeUtc: t5,
            roomExitTimeUtc: t6,
            anesthesiaType: "General",
            anesthesiaNotes: "Propofol ve sevofluran indüksiyon ve idame. Entübasyon sorunsuz.",
            intraoperativeFindings: "Akut kolesistit bulguları, hidropik safra kesesi, yapışıklıklar ayrıldı.",
            intraoperativeComplications: "Komplikasyon gelişmedi.",
            estimatedBloodLossMl: 40,
            specimensCollected: "Safra Kesesi (Patoloji)",
            countsConfirmed: true,
            postOpDisposition: "PACU",
            postOpInstructions: "PACU takibi, 6 saat oral stop, vital bulgular 15 dk arayla.");

        var saveResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/surgery/perioperative-records", saveReq);
        Assert.Equal(HttpStatusCode.OK, saveResp.StatusCode);
        var record = await saveResp.Content.ReadFromJsonAsync<PerioperativeRecordResponse>();
        Assert.NotNull(record);
        Assert.False(record.IsSigned);
        Assert.Equal(40, record.EstimatedBloodLossMl);
        Assert.True(record.CountsConfirmed);

        // 4. Sign Perioperative Record
        var signResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/surgery/perioperative-records/{record.Id}/sign",
            new SignPerioperativeRecordRequest());

        Assert.Equal(HttpStatusCode.OK, signResp.StatusCode);
        var signedRecord = await signResp.Content.ReadFromJsonAsync<PerioperativeRecordResponse>();
        Assert.NotNull(signedRecord);
        Assert.True(signedRecord.IsSigned);
        Assert.NotNull(signedRecord.SignedAtUtc);

        // 5. Attempt direct edit after signing -> 409 Conflict
        var editAttemptResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/surgery/perioperative-records", saveReq);
        Assert.Equal(HttpStatusCode.Conflict, editAttemptResp.StatusCode);

        // 6. Add Correction
        var corrReq = new AddPerioperativeCorrectionRequest(
            reasonForCorrection: "Drenaj miktarı eklemesi",
            correctionNote: "Operasyon bitiminde loja yerleştirilen hemovac drenden 30 cc seroanjinöz mayi geldiği gözlendi.");

        var corrResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/surgery/perioperative-records/{record.Id}/corrections",
            corrReq);

        Assert.Equal(HttpStatusCode.OK, corrResp.StatusCode);
        var corr = await corrResp.Content.ReadFromJsonAsync<PerioperativeCorrectionResponse>();
        Assert.NotNull(corr);
        Assert.Equal("Drenaj miktarı eklemesi", corr.ReasonForCorrection);

        // 7. Verify GET by booking contains correction history
        var getByBookingResp = await doctorClient.GetFromJsonAsync<PerioperativeRecordResponse>(
            $"/api/v1/surgery/perioperative-records/by-booking/{booking.Id}");

        Assert.NotNull(getByBookingResp);
        Assert.True(getByBookingResp.IsSigned);
        Assert.Single(getByBookingResp.Corrections);
        Assert.Equal("Drenaj miktarı eklemesi", getByBookingResp.Corrections[0].ReasonForCorrection);
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
