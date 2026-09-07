using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Interoperability;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using HospitalManagement.Modules.Interoperability.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class MhrsIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G05")]
    public async Task MhrsQuerySlotsReturnsAvailableSlots()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var resp = await doctorClient.GetAsync("/api/v1/interoperability/mhrs/slots?clinicCode=KARD-01");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var slots = await resp.Content.ReadFromJsonAsync<List<MhrsSlotResponse>>();
        Assert.NotNull(slots);
        Assert.NotEmpty(slots);
        Assert.All(slots, s => Assert.Equal("KARD-01", s.ClinicCode));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G05")]
    [Trait("Gate", "F10-KAPI")]
    public async Task MhrsBookingWithIdempotencyAndConflictResolution()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var docId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var slotId = "SLOT-20260901-0930-0102";
        var idempKey = "IDEMP-INTEG-MHRS-001";

        var bookReq = new MhrsBookAppointmentRequest
        {
            SlotId = slotId,
            PatientNationalId = "11111111110",
            PatientFullName = "DEMO HASTA BIR",
            DoctorId = docId,
            DoctorName = "Dr. Ahmet Tabip",
            ClinicName = "Kardiyoloji Polikliniği",
            AppointmentDateTimeUtc = new DateTime(2026, 9, 1, 9, 30, 0, DateTimeKind.Utc),
            IdempotencyKey = idempKey,
        };

        // 1. Initial Booking
        var bookResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/interoperability/mhrs/appointments", bookReq);
        Assert.Equal(HttpStatusCode.OK, bookResp.StatusCode);
        var booked = await bookResp.Content.ReadFromJsonAsync<MhrsAppointmentResponse>();
        Assert.NotNull(booked);
        Assert.Equal(slotId, booked.SlotId);
        Assert.Equal("Booked", booked.Status);

        // 2. Idempotent Retry with same key
        var retryResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/interoperability/mhrs/appointments", bookReq);
        Assert.Equal(HttpStatusCode.OK, retryResp.StatusCode);
        var retryBooked = await retryResp.Content.ReadFromJsonAsync<MhrsAppointmentResponse>();
        Assert.NotNull(retryBooked);
        Assert.Equal(booked.MhrsAppointmentId, retryBooked.MhrsAppointmentId);

        // 3. Conflict Booking: different patient, different idempotency key, but SAME slot
        var conflictReq = new MhrsBookAppointmentRequest
        {
            SlotId = slotId,
            PatientNationalId = "22222222220",
            PatientFullName = "DEMO HASTA IKI",
            DoctorId = docId,
            DoctorName = "Dr. Ahmet Tabip",
            ClinicName = "Kardiyoloji Polikliniği",
            AppointmentDateTimeUtc = new DateTime(2026, 9, 1, 9, 30, 0, DateTimeKind.Utc),
            IdempotencyKey = "IDEMP-INTEG-MHRS-002",
        };

        var conflictResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/interoperability/mhrs/appointments", conflictReq);
        Assert.True(conflictResp.StatusCode == HttpStatusCode.InternalServerError || !conflictResp.IsSuccessStatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G05")]
    public async Task MhrsCancellationAndSyncWorkCorrectly()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var docId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var slotId = "SLOT-20260901-1000-0102";

        var bookReq = new MhrsBookAppointmentRequest
        {
            SlotId = slotId,
            PatientNationalId = "33333333330",
            PatientFullName = "DEMO HASTA UC",
            DoctorId = docId,
            DoctorName = "Dr. Ahmet Tabip",
            ClinicName = "Kardiyoloji Polikliniği",
            AppointmentDateTimeUtc = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            IdempotencyKey = "IDEMP-INTEG-MHRS-003",
        };

        var bookResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/interoperability/mhrs/appointments", bookReq);
        Assert.Equal(HttpStatusCode.OK, bookResp.StatusCode);
        var booked = await bookResp.Content.ReadFromJsonAsync<MhrsAppointmentResponse>();
        Assert.NotNull(booked);

        // 1. Cancel
        var cancelReq = new MhrsCancelAppointmentRequest
        {
            Reason = "Hasta gelemeyeceğini bildirdi",
            IsDoctor = false,
        };

        var cancelResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/interoperability/mhrs/appointments/{booked.MhrsAppointmentId}/cancel", cancelReq);
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);
        var cancelled = await cancelResp.Content.ReadFromJsonAsync<MhrsAppointmentResponse>();
        Assert.NotNull(cancelled);
        Assert.Equal("CancelledByPatient", cancelled.Status);
        Assert.Equal("Hasta gelemeyeceğini bildirdi", cancelled.CancellationReason);

        // 2. Query Patient Appointments
        var patResp = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/interoperability/mhrs/patient-appointments/search",
            new MhrsPatientAppointmentsQueryRequest { PatientNationalId = "33333333330" });
        Assert.Equal(HttpStatusCode.OK, patResp.StatusCode);
        var patList = await patResp.Content.ReadFromJsonAsync<List<MhrsAppointmentResponse>>();
        Assert.NotNull(patList);
        Assert.Single(patList);

        // 3. Sync Schedule
        var syncResp = await PostWithAntiforgeryAsync<object?>(doctorClient, "/api/v1/interoperability/mhrs/sync?syncDate=2026-09-01", null);
        Assert.Equal(HttpStatusCode.OK, syncResp.StatusCode);
        var syncSummary = await syncResp.Content.ReadFromJsonAsync<MhrsSyncSummaryResponse>();
        Assert.NotNull(syncSummary);
        Assert.True(syncSummary.TotalSynced >= 1);
        Assert.True(syncSummary.CancelledAppointments >= 1);
    }

    private static HttpClient CreateSecureClient(WebApplicationFactory<Program> application)
    {
        return application.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password)
    {
        var antiforgeryResp = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/v1/identity/antiforgery");
        Assert.NotNull(antiforgeryResp);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/sessions")
        {
            Content = JsonContent.Create(new LoginRequest
            {
                Email = email,
                Password = password,
            }),
        };
        request.Headers.Add("X-HMS-CSRF", antiforgeryResp.Token);

        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostWithAntiforgeryAsync<T>(
        HttpClient client,
        string url,
        T body)
    {
        var antiforgeryResp = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/v1/identity/antiforgery");
        Assert.NotNull(antiforgeryResp);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = body is not null ? JsonContent.Create(body) : null,
        };
        request.Headers.Add("X-HMS-CSRF", antiforgeryResp.Token);

        return await client.SendAsync(request);
    }

    private static async Task RunAllMigrationsAndSeedAsync(WebApplicationFactory<Program> application)
    {
        using var scope = application.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var idDb = sp.GetRequiredService<IdentityAccessDbContext>();
        await idDb.Database.MigrateAsync();

        var audDb = sp.GetRequiredService<AuditPrivacyDbContext>();
        await audDb.Database.MigrateAsync();

        var orgDb = sp.GetRequiredService<OrganizationDbContext>();
        await orgDb.Database.MigrateAsync();

        var patDb = sp.GetRequiredService<PatientsDbContext>();
        await patDb.Database.MigrateAsync();

        var schDb = sp.GetRequiredService<SchedulingDbContext>();
        await schDb.Database.MigrateAsync();

        var notDb = sp.GetRequiredService<NotificationsDbContext>();
        await notDb.Database.MigrateAsync();

        var clinDb = sp.GetRequiredService<ClinicalRecordsDbContext>();
        await clinDb.Database.MigrateAsync();

        var pharmDb = sp.GetRequiredService<PharmacyDbContext>();
        await pharmDb.Database.MigrateAsync();

        var diagDb = sp.GetRequiredService<DiagnosticsDbContext>();
        await diagDb.Database.MigrateAsync();

        var inpDb = sp.GetRequiredService<InpatientDbContext>();
        await inpDb.Database.MigrateAsync();

        var surgDb = sp.GetRequiredService<SurgeryDbContext>();
        await surgDb.Database.MigrateAsync();

        var specDb = sp.GetRequiredService<SpecialtyCareDbContext>();
        await specDb.Database.MigrateAsync();

        var interopDb = sp.GetRequiredService<InteroperabilityDbContext>();
        await interopDb.Database.MigrateAsync();

        var idSeeder = sp.GetRequiredService<IIdentityDataSeeder>();
        await idSeeder.SeedAsync();

        var orgSeeder = sp.GetRequiredService<IOrganizationDataSeeder>();
        await orgSeeder.SeedAsync();
    }
}
