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

public sealed class EmergencyTrackingBoardIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-G02")]
    public async Task EmergencyTrackingBoardSummaryAndWorklistReflectLifecycleEvents()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000121");
        var doctorPersonId = Guid.Parse("00000000-0000-0000-0000-000000000102");

        var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        // 1. Initial Summary Check
        var initialSummaryResp = await nurseClient.GetAsync("/api/v1/emergency/board/summary");
        Assert.Equal(HttpStatusCode.OK, initialSummaryResp.StatusCode);
        var initialSummary = await initialSummaryResp.Content.ReadFromJsonAsync<EmergencyBoardSummaryResponse>();
        Assert.NotNull(initialSummary);
        var baseActiveCount = initialSummary.TotalActiveAdmissions;
        var baseWaitingTriage = initialSummary.WaitingTriageCount;

        // 2. Nurse creates new Walk-in Admission
        var createReq = new CreateEmergencyAdmissionRequest
        {
            PatientId = patientId,
            ArrivalType = "WalkIn",
            ChiefComplaint = "Şiddetli nefes darlığı ve göğüs sıkışması",
        };
        var createResp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/emergency/admissions", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var admission = await createResp.Content.ReadFromJsonAsync<EmergencyAdmissionResponse>();
        Assert.NotNull(admission);

        // 3. Board Summary should now have 1 more active admission & waiting triage
        var summaryResp2 = await nurseClient.GetAsync("/api/v1/emergency/board/summary");
        Assert.Equal(HttpStatusCode.OK, summaryResp2.StatusCode);
        var summary2 = await summaryResp2.Content.ReadFromJsonAsync<EmergencyBoardSummaryResponse>();
        Assert.NotNull(summary2);
        Assert.Equal(baseActiveCount + 1, summary2.TotalActiveAdmissions);
        Assert.Equal(baseWaitingTriage + 1, summary2.WaitingTriageCount);

        // 4. Worklist check - should contain new admission
        var worklistResp1 = await nurseClient.GetAsync("/api/v1/emergency/board/worklist");
        Assert.Equal(HttpStatusCode.OK, worklistResp1.StatusCode);
        var worklist1 = await worklistResp1.Content.ReadFromJsonAsync<List<EmergencyBoardWorklistItemResponse>>();
        Assert.NotNull(worklist1);
        Assert.Contains(worklist1, i => i.AdmissionId == admission.Id && i.Status == "WaitingTriage");

        // 5. Nurse records Triage as Red2Emergency
        var triageReq = new RecordTriageRequest
        {
            TriageLevel = "Red2Emergency",
            TriageCategoryReason = "Taşikardi ve SpO2 %89",
            OxygenSaturationPercent = 89,
            HeartRate = 125,
            SystolicBp = 145,
            DiastolicBp = 95,
            RespiratoryRate = 26,
            Consciousness = "Alert",
        };
        var triageResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/emergency/admissions/{admission.Id}/triage", triageReq);
        Assert.Equal(HttpStatusCode.OK, triageResp.StatusCode);

        // 6. Summary reflects Red2Count increment
        var summaryResp3 = await nurseClient.GetAsync("/api/v1/emergency/board/summary");
        var summary3 = await summaryResp3.Content.ReadFromJsonAsync<EmergencyBoardSummaryResponse>();
        Assert.NotNull(summary3);
        Assert.Equal(1, summary3.Red2Count);
        Assert.Equal(baseWaitingTriage, summary3.WaitingTriageCount); // back to original waiting triage count

        // 7. Assign Doctor & Zone "Kırmızı Alan - Resüsitasyon 2"
        var assignReq = new AssignEmergencyDoctorRequest
        {
            DoctorId = doctorPersonId,
            BedOrZone = "Kırmızı Alan - Resüsitasyon 2",
        };
        var assignResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/emergency/admissions/{admission.Id}/assign-doctor", assignReq);
        Assert.Equal(HttpStatusCode.OK, assignResp.StatusCode);

        // 8. Worklist filter by Zone "Kırmızı"
        var zoneResp = await nurseClient.GetAsync("/api/v1/emergency/board/worklist?zone=K%C4%B1rm%C4%B1z%C4%B1");
        Assert.Equal(HttpStatusCode.OK, zoneResp.StatusCode);
        var zoneList = await zoneResp.Content.ReadFromJsonAsync<List<EmergencyBoardWorklistItemResponse>>();
        Assert.NotNull(zoneList);
        Assert.Contains(zoneList, i => i.AdmissionId == admission.Id && i.AssignedBedOrZone == "Kırmızı Alan - Resüsitasyon 2");

        // 9. Discharge Patient
        var dischargeReq = new UpdateEmergencyAdmissionStatusRequest
        {
            Status = "Discharged",
            Notes = "Bronkodilatör tedavi sonrası stabil, taburcu edildi.",
        };
        var statusResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/emergency/admissions/{admission.Id}/status", dischargeReq);
        Assert.Equal(HttpStatusCode.OK, statusResp.StatusCode);

        // 10. Summary reflects Discharged count increment & active count return to base
        var summaryResp4 = await nurseClient.GetAsync("/api/v1/emergency/board/summary");
        var summary4 = await summaryResp4.Content.ReadFromJsonAsync<EmergencyBoardSummaryResponse>();
        Assert.NotNull(summary4);
        Assert.Equal(baseActiveCount, summary4.TotalActiveAdmissions);
        Assert.True(summary4.TodayDischargedCount >= 1);
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
