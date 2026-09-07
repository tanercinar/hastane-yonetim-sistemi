using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Interoperability;
using HospitalManagement.Host.Authorization;
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

public sealed class DicomPacsIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G04")]
    public async Task DicomWorklistQueryReturnsItemsAndFiltersByModality()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var radtechClient = CreateSecureClient(application);
        var radLogin = await LoginAsync(radtechClient, "DEMO-radtech@hospital.invalid", "DEMO-RadTech-Pass!1");
        Assert.Equal(HttpStatusCode.OK, radLogin.StatusCode);

        // 1. Query all MWL items
        var allResp = await radtechClient.GetAsync("/api/v1/interoperability/dicom/worklist");
        Assert.Equal(HttpStatusCode.OK, allResp.StatusCode);
        var allItems = await allResp.Content.ReadFromJsonAsync<List<DicomWorklistItemResponse>>();
        Assert.NotNull(allItems);
        Assert.NotEmpty(allItems);

        // 2. Query filtered by CT
        var ctResp = await radtechClient.GetAsync("/api/v1/interoperability/dicom/worklist?modality=CT");
        Assert.Equal(HttpStatusCode.OK, ctResp.StatusCode);
        var ctItems = await ctResp.Content.ReadFromJsonAsync<List<DicomWorklistItemResponse>>();
        Assert.NotNull(ctItems);
        Assert.All(ctItems, i => Assert.Equal("CT", i.Modality));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G04")]
    public async Task DicomWorklistCreateOrderAndStudyQueryWorkSuccessfully()
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

        var patId = Guid.Parse("00000000-0000-0000-0000-000000000121");
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        EstablishCareRelationship(application, doctorId, patId);

        // 1. Create MWL Order
        var orderReq = new CreateDicomWorklistOrderRequest
        {
            PatientId = patId,
            Modality = "MR",
            ProcedureDescription = "Kranial MRG Kontrastlı",
            AeTitle = "MR_SCANNER_02",
        };

        var orderResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/interoperability/dicom/worklist", orderReq);
        Assert.Equal(HttpStatusCode.OK, orderResp.StatusCode);
        var created = await orderResp.Content.ReadFromJsonAsync<DicomWorklistItemResponse>();
        Assert.NotNull(created);
        Assert.Equal("MR", created.Modality);
        Assert.Equal("Scheduled", created.Status);
        Assert.NotEmpty(created.StudyInstanceUid);

        // 2. Query Study Metadata (RadiologyStaff)
        var radtechClient = CreateSecureClient(application);
        var radLogin = await LoginAsync(radtechClient, "DEMO-radtech@hospital.invalid", "DEMO-RadTech-Pass!1");
        Assert.Equal(HttpStatusCode.OK, radLogin.StatusCode);

        var studyResp = await radtechClient.GetAsync($"/api/v1/interoperability/dicom/studies/{created.StudyInstanceUid}");
        Assert.Equal(HttpStatusCode.OK, studyResp.StatusCode);
        var study = await studyResp.Content.ReadFromJsonAsync<DicomStudyMetadataResponse>();
        Assert.NotNull(study);
        Assert.Equal(created.StudyInstanceUid, study.StudyInstanceUid);
        Assert.NotEmpty(study.Series);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G04")]
    public async Task DicomPacsOfflineFailsGracefully()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var adminClient = CreateSecureClient(application);
        var adminLogin = await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1");
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        // 1. Set DicomPacs FaultMode to Offline
        var updateReq = new UpdateMockServerConfigRequest
        {
            SystemType = "DicomPacs",
            IsEnabled = true,
            FaultMode = "Offline",
            LatencyMilliseconds = 0,
            FailureRatePercentage = 0,
            MaxRetryAttempts = 0,
            TimeoutSeconds = 5,
        };

        var updateResp = await PutWithAntiforgeryAsync(adminClient, "/api/v1/interoperability/mock-engine/configs/DicomPacs", updateReq);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        // 2. Query MWL as RadiologyStaff -> Should Fail with 500 or error due to Offline mock engine
        var radtechClient = CreateSecureClient(application);
        var radLogin = await LoginAsync(radtechClient, "DEMO-radtech@hospital.invalid", "DEMO-RadTech-Pass!1");
        Assert.Equal(HttpStatusCode.OK, radLogin.StatusCode);

        var queryResp = await radtechClient.GetAsync("/api/v1/interoperability/dicom/worklist");
        Assert.True(queryResp.StatusCode == HttpStatusCode.InternalServerError || !queryResp.IsSuccessStatusCode);

        // 3. Reset Circuit Breaker (admin)
        var resetResp = await PostWithAntiforgeryAsync<object?>(adminClient, "/api/v1/interoperability/mock-engine/reset-circuit/DicomPacs", null);
        Assert.Equal(HttpStatusCode.NoContent, resetResp.StatusCode);
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

    private static async Task<HttpResponseMessage> PutWithAntiforgeryAsync<T>(
        HttpClient client,
        string url,
        T body)
    {
        var antiforgeryResp = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/v1/identity/antiforgery");
        Assert.NotNull(antiforgeryResp);

        using var request = new HttpRequestMessage(HttpMethod.Put, url)
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

    private static void EstablishCareRelationship(
        WebApplicationFactory<Program> application,
        Guid clinicianId,
        Guid patientId)
    {
        using var scope = application.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CareRelationshipRegistry>()
            .EstablishCareRelationship(clinicianId, patientId);
    }
}
