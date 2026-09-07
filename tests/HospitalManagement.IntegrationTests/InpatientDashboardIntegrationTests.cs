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

public sealed class InpatientDashboardIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F07-G08")]
    public async Task InpatientDashboardReflectsRealtimeOccupancyAndLifecycleChanges()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var doctorPersonId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var cardDeptId = Guid.Parse("30000000-0000-0000-0000-000000000003");

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // 1. Initial dashboard state
        var initialDashboard = await doctorClient.GetFromJsonAsync<InpatientDashboardResponse>("/api/v1/inpatient/dashboard");
        Assert.NotNull(initialDashboard);
        Assert.True(initialDashboard.TotalBeds > 0);
        Assert.Equal(0, initialDashboard.OccupiedBeds);
        Assert.Equal(0.0, initialDashboard.OverallOccupancyPercentage);
        Assert.Equal(0, initialDashboard.PendingAdmissionsCount);
        Assert.Equal(0, initialDashboard.TodayDischargesCount);

        // 2. Doctor requests admission -> PendingAdmissionsCount becomes 1
        var wards = await doctorClient.GetFromJsonAsync<List<WardResponse>>("/api/v1/inpatient/wards");
        Assert.NotNull(wards);
        var cardWard = wards.Single(w => w.Code == "DEMO-WRD-CARD");

        var createAdmissionReq = new CreateAdmissionRequest
        {
            PatientId = patientId,
            DepartmentId = cardDeptId,
            AdmittingWardId = cardWard.Id,
            AttendingDoctorId = doctorPersonId,
            AdmissionReason = "Akut Miyokard İnfarktüsü",
            DietType = "LowSodium",
            FallRiskScore = 35,
            IsolationRequired = "None",
            EstimatedStayDays = 3,
        };

        var admReqResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/admissions", createAdmissionReq);
        Assert.Equal(HttpStatusCode.Created, admReqResp.StatusCode);
        var admission = await admReqResp.Content.ReadFromJsonAsync<AdmissionResponse>();
        Assert.NotNull(admission);

        var dashboardWithPending = await doctorClient.GetFromJsonAsync<InpatientDashboardResponse>("/api/v1/inpatient/dashboard");
        Assert.NotNull(dashboardWithPending);
        Assert.Equal(1, dashboardWithPending.PendingAdmissionsCount);

        // 3. Nurse accepts and admits patient to Bed -> OccupiedBeds becomes 1, OccupancyPercentage > 0
        var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{admission.Id}/accept", new AcceptAdmissionRequest());

        var availableBeds = await nurseClient.GetFromJsonAsync<List<BedResponse>>($"/api/v1/inpatient/beds?wardId={cardWard.Id}&status=Available");
        Assert.NotNull(availableBeds);
        var bed = availableBeds.First();

        await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{admission.Id}/admit", new AdmitPatientRequest { BedId = bed.Id });

        var dashboardAdmitted = await doctorClient.GetFromJsonAsync<InpatientDashboardResponse>("/api/v1/inpatient/dashboard");
        Assert.NotNull(dashboardAdmitted);
        Assert.Equal(1, dashboardAdmitted.OccupiedBeds);
        Assert.True(dashboardAdmitted.OverallOccupancyPercentage > 0);
        Assert.Equal(0, dashboardAdmitted.PendingAdmissionsCount);

        var cardWardSummary = dashboardAdmitted.Wards.Single(w => w.WardId == cardWard.Id);
        Assert.Equal(1, cardWardSummary.OccupiedBeds);
        Assert.Equal(1, cardWardSummary.ActivePatientsCount);

        // 4. Doctor discharges patient -> Occupied becomes 0, Cleaning becomes 1, TodayDischarges becomes 1
        var dischargeReq = new DischargeAdmissionRequest
        {
            AdmissionId = admission.Id,
            DischargeType = "Home",
            DischargeSummary = "Hasta tedavisi tamamlanarak şifa ile taburcu edilmiştir ve takibe alınmıştır.",
            FinalDiagnosisCode = "I25.1",
            FinalDiagnosisDescription = "Aterosklerotik Kalp Hastalığı",
            DischargeRecommendations = "Tuzsuz diyet ve 1 hafta sonra kontrol.",
            DischargePrescriptionSummary = "DEMO-Aspirin 100mg 1x1",
            FollowUpAppointmentDateUtc = DateTime.UtcNow.AddDays(7),
        };

        var dischargeResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/discharges", dischargeReq);
        Assert.Equal(HttpStatusCode.Created, dischargeResp.StatusCode);

        var dashboardDischarged = await doctorClient.GetFromJsonAsync<InpatientDashboardResponse>("/api/v1/inpatient/dashboard");
        Assert.NotNull(dashboardDischarged);
        Assert.Equal(0, dashboardDischarged.OccupiedBeds);
        Assert.Equal(0.0, dashboardDischarged.OverallOccupancyPercentage);
        Assert.Equal(1, dashboardDischarged.CleaningBeds);
        Assert.Equal(1, dashboardDischarged.TodayDischargesCount);
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
