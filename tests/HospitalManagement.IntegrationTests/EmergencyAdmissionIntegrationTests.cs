using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Emergency;
using HospitalManagement.Contracts.Identity;
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
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class EmergencyAdmissionIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-G01")]
    public async Task EmergencyAdmissionLifecycleCreateTriageAssignDoctorDischargeSucceeds()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000111");
        var doctorPersonId = Guid.Parse("00000000-0000-0000-0000-000000000102");

        // 1. Nurse logs in and creates Walk-in Emergency Admission
        var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        var createReq = new CreateEmergencyAdmissionRequest
        {
            PatientId = patientId,
            ArrivalType = "WalkIn",
            ChiefComplaint = "Akut batın ağrısı ve bulantı",
            AdmissionNotes = "Ayaktan triyaja başvurdu.",
        };

        var createResp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/emergency/admissions", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var admission = await createResp.Content.ReadFromJsonAsync<EmergencyAdmissionResponse>();
        Assert.NotNull(admission);
        Assert.Equal("WaitingTriage", admission.Status);
        Assert.StartsWith("DEMO-EMG-", admission.EmergencyProtocolNumber, StringComparison.Ordinal);

        // 2. Nurse records triage with vitals and educational classification
        var triageReq = new RecordTriageRequest
        {
            TriageLevel = "YellowUrgent",
            TriageCategoryReason = "Sağ alt kadranda şiddetli hassasiyet, akut apandisit şüphesi.",
            SystolicBp = 125,
            DiastolicBp = 80,
            HeartRate = 92,
            BodyTemperatureCelsius = 37.8m,
            RespiratoryRate = 18,
            OxygenSaturationPercent = 98,
            PainScale = 7,
            Consciousness = "Alert",
            ClinicalNotes = "Hasta cerrahi acil alanına alındı.",
        };

        var triageResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/emergency/admissions/{admission.Id}/triage", triageReq);
        Assert.Equal(HttpStatusCode.OK, triageResp.StatusCode);
        var triaged = await triageResp.Content.ReadFromJsonAsync<EmergencyAdmissionResponse>();
        Assert.NotNull(triaged);
        Assert.Equal("TriagedWaitingDoctor", triaged.Status);
        Assert.NotNull(triaged.Triage);
        Assert.Equal("YellowUrgent", triaged.Triage.TriageLevel);
        Assert.True(triaged.Triage.EducationalClassificationAssisted);
        Assert.Equal(7, triaged.Triage.PainScale);

        // 3. Doctor logs in and assigns doctor & bed/zone
        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var assignReq = new AssignEmergencyDoctorRequest
        {
            DoctorId = doctorPersonId,
            BedOrZone = "Sarı Alan - Yatak 4",
        };

        var assignResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/emergency/admissions/{admission.Id}/assign-doctor", assignReq);
        Assert.Equal(HttpStatusCode.OK, assignResp.StatusCode);
        var assigned = await assignResp.Content.ReadFromJsonAsync<EmergencyAdmissionResponse>();
        Assert.NotNull(assigned);
        Assert.Equal("InEvaluation", assigned.Status);
        Assert.Equal(doctorPersonId, assigned.AssignedDoctorId);
        Assert.Equal("Sarı Alan - Yatak 4", assigned.AssignedBedOrZone);

        // 4. Update status to InObservation
        var statusObsReq = new UpdateEmergencyAdmissionStatusRequest
        {
            Status = "InObservation",
            Notes = "Laboratuvar ve USG sonuçları bekleniyor.",
        };
        var obsResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/emergency/admissions/{admission.Id}/status", statusObsReq);
        Assert.Equal(HttpStatusCode.OK, obsResp.StatusCode);

        // 5. Complete disposition / discharge
        var statusDischargeReq = new UpdateEmergencyAdmissionStatusRequest
        {
            Status = "Discharged",
            Notes = "Cerrahi patoloji dışlandı, semptomatik tedavi sonrası şifa ile taburcu edildi.",
        };
        var dischargeResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/emergency/admissions/{admission.Id}/status", statusDischargeReq);
        Assert.Equal(HttpStatusCode.OK, dischargeResp.StatusCode);
        var discharged = await dischargeResp.Content.ReadFromJsonAsync<EmergencyAdmissionResponse>();
        Assert.NotNull(discharged);
        Assert.Equal("Discharged", discharged.Status);
        Assert.NotNull(discharged.CompletedAtUtc);

        // 6. Verify full detail query
        var detail = await doctorClient.GetFromJsonAsync<EmergencyAdmissionResponse>($"/api/v1/emergency/admissions/{admission.Id}");
        Assert.NotNull(detail);
        Assert.Equal("Discharged", detail.Status);
        Assert.Equal("Cerrahi patoloji dışlandı, semptomatik tedavi sonrası şifa ile taburcu edildi.", detail.DischargeOrDispositionNotes);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-G01")]
    public async Task EmergencyAdmissionInvariantsEnforcesBusinessRules()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000112");

        var nurseClient = CreateSecureClient(application);
        await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");

        // 1. Initial valid admission
        var createReq = new CreateEmergencyAdmissionRequest
        {
            PatientId = patientId,
            ArrivalType = "WalkIn",
            ChiefComplaint = "Baş dönmesi",
        };
        var resp1 = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/emergency/admissions", createReq);
        Assert.Equal(HttpStatusCode.Created, resp1.StatusCode);

        // 2. Duplicate active admission for same patient returns 409 Conflict
        var dupResp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/emergency/admissions", createReq);
        Assert.Equal(HttpStatusCode.Conflict, dupResp.StatusCode);

        // 3. Empty chief complaint returns 400 Bad Request
        var emptyReq = new CreateEmergencyAdmissionRequest
        {
            PatientId = Guid.Parse("00000000-0000-0000-0000-000000000113"),
            ArrivalType = "WalkIn",
            ChiefComplaint = "   ",
        };
        var emptyResp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/emergency/admissions", emptyReq);
        Assert.Equal(HttpStatusCode.BadRequest, emptyResp.StatusCode);
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

    private static async Task RunAllMigrationsAndSeedAsync(ApiWebApplicationFactory application)
    {
        await using var scope = application.Services.CreateAsyncScope();

        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
        await identityDb.Database.MigrateAsync();

        var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        await auditDb.Database.MigrateAsync();

        var orgDb = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        await orgDb.Database.MigrateAsync();

        var patientsDb = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();
        await patientsDb.Database.MigrateAsync();

        var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        await schedulingDb.Database.MigrateAsync();

        var notificationsDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        await notificationsDb.Database.MigrateAsync();

        var clinicalDb = scope.ServiceProvider.GetRequiredService<ClinicalRecordsDbContext>();
        await clinicalDb.Database.MigrateAsync();

        var pharmacyDb = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        await pharmacyDb.Database.MigrateAsync();

        var diagnosticsDb = scope.ServiceProvider.GetRequiredService<DiagnosticsDbContext>();
        await diagnosticsDb.Database.MigrateAsync();

        var inpatientDb = scope.ServiceProvider.GetRequiredService<InpatientDbContext>();
        await inpatientDb.Database.MigrateAsync();

        var emergencyDb = scope.ServiceProvider.GetRequiredService<EmergencyDbContext>();
        await emergencyDb.Database.MigrateAsync();

        var orgSeeder = scope.ServiceProvider.GetRequiredService<IOrganizationDataSeeder>();
        await orgSeeder.SeedAsync();

        var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
        await identitySeeder.SeedAsync();

        var inpatientSeeder = scope.ServiceProvider.GetRequiredService<IInpatientDataSeeder>();
        await inpatientSeeder.SeedAsync();

        var emergencySeeder = scope.ServiceProvider.GetRequiredService<IEmergencyDataSeeder>();
        await emergencySeeder.SeedAsync();
    }
}
