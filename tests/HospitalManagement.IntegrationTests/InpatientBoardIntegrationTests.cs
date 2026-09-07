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

public sealed class InpatientBoardIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F07-G04")]
    public async Task InpatientClinicalBoardReturnsScopedPatientsWithCorrectRiskAndIsolationAlerts()
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

        // 1. Doctor requests admission with high fall risk and Contact isolation
        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var wardsResponse = await doctorClient.GetAsync("/api/v1/inpatient/wards");
        Assert.Equal(HttpStatusCode.OK, wardsResponse.StatusCode);
        var wards = await wardsResponse.Content.ReadFromJsonAsync<List<WardResponse>>();
        Assert.NotNull(wards);
        var cardWard = wards.Single(w => w.Code == "DEMO-WRD-CARD");

        var createAdmissionReq = new CreateAdmissionRequest
        {
            PatientId = patientId,
            DepartmentId = cardDeptId,
            AdmittingWardId = cardWard.Id,
            AttendingDoctorId = doctorPersonId,
            AdmissionReason = "Akut İskemi ve Düşme Riski",
            DiagnosisCode = "I25.1",
            DiagnosisDescription = "Aterosklerotik Kalp Hastalığı",
            DietType = "Diabetic",
            FallRiskScore = 65,
            IsolationRequired = "Contact",
            EstimatedStayDays = 4,
        };

        var reqResponse = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/admissions", createAdmissionReq);
        Assert.Equal(HttpStatusCode.Created, reqResponse.StatusCode);
        var createdAdmission = await reqResponse.Content.ReadFromJsonAsync<AdmissionResponse>();
        Assert.NotNull(createdAdmission);

        // 2. Nurse accepts and admits patient
        var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{createdAdmission.Id}/accept", new AcceptAdmissionRequest());

        var bedsResponse = await nurseClient.GetAsync($"/api/v1/inpatient/beds?wardId={cardWard.Id}&status=Available");
        var availableBeds = await bedsResponse.Content.ReadFromJsonAsync<List<BedResponse>>();
        Assert.NotNull(availableBeds);
        Assert.NotEmpty(availableBeds);
        var selectedBed = availableBeds[0];

        await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{createdAdmission.Id}/admit", new AdmitPatientRequest { BedId = selectedBed.Id });

        // 3. Nurse views the Clinical Board
        var boardResponse = await nurseClient.GetAsync($"/api/v1/inpatient/board?wardId={cardWard.Id}");
        Assert.Equal(HttpStatusCode.OK, boardResponse.StatusCode);
        var boardItems = await boardResponse.Content.ReadFromJsonAsync<List<InpatientBoardItemResponse>>();
        Assert.NotNull(boardItems);
        Assert.NotEmpty(boardItems);

        var item = boardItems.Single(i => i.AdmissionId == createdAdmission.Id);
        Assert.Equal(patientId, item.PatientId);
        Assert.Equal("High", item.FallRiskLevel);
        Assert.Equal(65, item.FallRiskScore);
        Assert.Equal("Contact", item.IsolationRequired);
        Assert.Equal(selectedBed.BedNumber, item.BedNumber);
        Assert.Equal("Diabetic", item.DietType);
        Assert.Equal("I25.1", item.DiagnosisCode);
        Assert.True(item.DaysInHospital >= 1);

        // 4. Test Risk Filter
        var highRiskResponse = await nurseClient.GetAsync("/api/v1/inpatient/board?riskLevel=High");
        var highRiskItems = await highRiskResponse.Content.ReadFromJsonAsync<List<InpatientBoardItemResponse>>();
        Assert.NotNull(highRiskItems);
        Assert.Contains(highRiskItems, i => i.AdmissionId == createdAdmission.Id);

        var lowRiskResponse = await nurseClient.GetAsync("/api/v1/inpatient/board?riskLevel=Low");
        var lowRiskItems = await lowRiskResponse.Content.ReadFromJsonAsync<List<InpatientBoardItemResponse>>();
        Assert.NotNull(lowRiskItems);
        Assert.DoesNotContain(lowRiskItems, i => i.AdmissionId == createdAdmission.Id);

        // 5. Test Patient Clinical Summary Endpoint
        var summaryResponse = await doctorClient.GetAsync($"/api/v1/inpatient/board/{createdAdmission.Id}/summary");
        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        var summary = await summaryResponse.Content.ReadFromJsonAsync<InpatientPatientSummaryResponse>();
        Assert.NotNull(summary);
        Assert.Equal(createdAdmission.Id, summary.AdmissionId);
        Assert.Equal(patientId, summary.PatientId);
        Assert.Equal("High", summary.FallRiskLevel);
        Assert.Equal("Contact", summary.IsolationRequired);
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
