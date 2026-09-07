using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Inpatient;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;
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

public sealed class InpatientDischargeIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F07-G07")]
    public async Task InpatientDischargeAndReferralLifecycleWorksEndToEndAndFreesBed()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patient1Id = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var patient2Id = Guid.Parse("00000000-0000-0000-0000-000000000110");
        var doctorPersonId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var cardDeptId = Guid.Parse("30000000-0000-0000-0000-000000000003");

        // 1. Doctor requests admission for Patient 1
        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var wards = await doctorClient.GetFromJsonAsync<List<WardResponse>>("/api/v1/inpatient/wards");
        Assert.NotNull(wards);
        var cardWard = wards.Single(w => w.Code == "DEMO-WRD-CARD");

        var createAdmissionReq1 = new CreateAdmissionRequest
        {
            PatientId = patient1Id,
            DepartmentId = cardDeptId,
            AdmittingWardId = cardWard.Id,
            AttendingDoctorId = doctorPersonId,
            AdmissionReason = "Akut Miyokard İnfarktüsü Sonrası Takip",
            DietType = "LowSodium",
            FallRiskScore = 30,
            IsolationRequired = "None",
            EstimatedStayDays = 3,
        };

        var admReqResponse1 = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/admissions", createAdmissionReq1);
        Assert.Equal(HttpStatusCode.Created, admReqResponse1.StatusCode);
        var admission1 = await admReqResponse1.Content.ReadFromJsonAsync<AdmissionResponse>();
        Assert.NotNull(admission1);

        // 2. Nurse accepts and admits patient 1 to bed
        var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        var acceptResp1 = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{admission1.Id}/accept", new AcceptAdmissionRequest());
        Assert.Equal(HttpStatusCode.OK, acceptResp1.StatusCode);

        var availableBeds1 = await nurseClient.GetFromJsonAsync<List<BedResponse>>($"/api/v1/inpatient/beds?wardId={cardWard.Id}&status=Available");
        Assert.NotNull(availableBeds1);
        var bed1 = availableBeds1.First();

        var admitResp1 = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{admission1.Id}/admit", new AdmitPatientRequest { BedId = bed1.Id });
        Assert.Equal(HttpStatusCode.OK, admitResp1.StatusCode);

        // Verify Bed 1 is Occupied
        var occupiedBed = await nurseClient.GetFromJsonAsync<BedResponse>($"/api/v1/inpatient/beds/{bed1.Id}");
        Assert.NotNull(occupiedBed);
        Assert.Equal("Occupied", occupiedBed.Status);

        // 3. Doctor processes Discharge to Home for Patient 1
        var dischargeReq1 = new DischargeAdmissionRequest
        {
            AdmissionId = admission1.Id,
            DischargeType = "Home",
            DischargeSummary = "Hasta yatışı süresince medikal tedavi almış olup klinik iyileşme sağlanarak şifa ile taburcu edilmiştir.",
            FinalDiagnosisCode = "I25.1",
            FinalDiagnosisDescription = "Aterosklerotik Kalp Hastalığı",
            DischargeRecommendations = "Düşük sodyumlu diyet, günlük tansiyon takibi ve 1 hafta sonra poliklinik kontrolü.",
            DischargePrescriptionSummary = "DEMO-Aspirin 100mg 1x1, DEMO-Metoprolol 25mg 1x1",
            FollowUpAppointmentDateUtc = DateTime.UtcNow.AddDays(7),
        };

        var dischargeResp1 = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/discharges", dischargeReq1);
        Assert.Equal(HttpStatusCode.Created, dischargeResp1.StatusCode);
        var discharge1 = await dischargeResp1.Content.ReadFromJsonAsync<InpatientDischargeResponse>();
        Assert.NotNull(discharge1);
        Assert.Equal("Home", discharge1.DischargeType);
        Assert.Equal("I25.1", discharge1.FinalDiagnosisCode);

        // 4. Verify Bed 1 is freed and marked as Cleaning
        var freedBed = await nurseClient.GetFromJsonAsync<BedResponse>($"/api/v1/inpatient/beds/{bed1.Id}");
        Assert.NotNull(freedBed);
        Assert.Equal("Cleaning", freedBed.Status);

        // 5. Verify Admission 1 is Discharged
        var dischargedAdmission = await nurseClient.GetFromJsonAsync<AdmissionResponse>($"/api/v1/inpatient/admissions/{admission1.Id}");
        Assert.NotNull(dischargedAdmission);
        Assert.Equal("Discharged", dischargedAdmission.Status);

        // 6. Query single discharge
        var singleDischarge = await doctorClient.GetFromJsonAsync<InpatientDischargeResponse>($"/api/v1/inpatient/discharges/{admission1.Id}");
        Assert.NotNull(singleDischarge);
        Assert.Equal(discharge1.Id, singleDischarge.Id);

        // 7. Patient 2 External Referral Discharge Flow
        var createAdmissionReq2 = new CreateAdmissionRequest
        {
            PatientId = patient2Id,
            DepartmentId = cardDeptId,
            AdmittingWardId = cardWard.Id,
            AttendingDoctorId = doctorPersonId,
            AdmissionReason = "Dirençli Kalp Yetersizliği",
            DietType = "Cardiac",
            FallRiskScore = 60,
            IsolationRequired = "Contact",
            EstimatedStayDays = 5,
        };

        var admReqResponse2 = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/admissions", createAdmissionReq2);
        Assert.Equal(HttpStatusCode.Created, admReqResponse2.StatusCode);
        var admission2 = await admReqResponse2.Content.ReadFromJsonAsync<AdmissionResponse>();
        Assert.NotNull(admission2);

        await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{admission2.Id}/accept", new AcceptAdmissionRequest());

        var availableBeds2 = await nurseClient.GetFromJsonAsync<List<BedResponse>>($"/api/v1/inpatient/beds?wardId={cardWard.Id}&status=Available");
        Assert.NotNull(availableBeds2);
        var bed2 = availableBeds2.First();

        await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{admission2.Id}/admit", new AdmitPatientRequest { BedId = bed2.Id });

        var dischargeReq2 = new DischargeAdmissionRequest
        {
            AdmissionId = admission2.Id,
            DischargeType = "TransferToOtherFacility",
            DischargeSummary = "Hastada ileri düzey mekanik dolaşım desteği ve transplantasyon değerlendirmesi gerektiğinden sevk edilmiştir.",
            FinalDiagnosisCode = "I50.9",
            FinalDiagnosisDescription = "Kalp Yetersizliği, Tanımlanmamış",
            DischargeRecommendations = "Ambulans ile nakil esnasında hemodinami ve oksijenasyon monitörizasyonu sürdürülmelidir.",
            TransferFacilityName = "DEMO-Ankara Şehir Hastanesi",
            TransferReason = "İleri kardiyak transplantasyon ve mekanik destek ünitesi ihtiyacı",
        };

        var dischargeResp2 = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/discharges", dischargeReq2);
        Assert.Equal(HttpStatusCode.Created, dischargeResp2.StatusCode);
        var discharge2 = await dischargeResp2.Content.ReadFromJsonAsync<InpatientDischargeResponse>();
        Assert.NotNull(discharge2);
        Assert.Equal("TransferToOtherFacility", discharge2.DischargeType);
        Assert.Equal("DEMO-Ankara Şehir Hastanesi", discharge2.TransferFacilityName);

        // 8. Query list of discharges
        var allDischarges = await doctorClient.GetFromJsonAsync<List<InpatientDischargeResponse>>("/api/v1/inpatient/discharges");
        Assert.NotNull(allDischarges);
        Assert.Equal(2, allDischarges.Count);

        // 9. Negative Test: Anonymous user cannot process discharge
        var anonClient = application.CreateClient();
        var anonMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/inpatient/discharges")
        {
            Content = JsonContent.Create(dischargeReq1),
        };
        var anonResp = await anonClient.SendAsync(anonMsg);
        Assert.True(anonResp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
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

        var orgSeeder = scope.ServiceProvider.GetRequiredService<IOrganizationDataSeeder>();
        await orgSeeder.SeedAsync();

        var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
        await identitySeeder.SeedAsync();

        var inpatientSeeder = scope.ServiceProvider.GetRequiredService<IInpatientDataSeeder>();
        await inpatientSeeder.SeedAsync();
    }
}
