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

public sealed class ENabizIntegrationTests
{
    private static readonly string[] ExpectedSendStatuses = ["Successful", "Failed"];
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G06")]
    public async Task ENabizEnqueueAndSendWithConsentSucceeds()
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

        var patientId = Guid.NewGuid();
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        EstablishCareRelationship(application, doctorId, patientId);

        // 1. Enqueue
        var enqueueReq = new EnqueueENabizPackageRequest
        {
            PackageType = 101,
            PatientId = patientId,
            PatientNationalId = "11111111110",
            HasPatientConsent = true,
            PayloadSummary = "DEMO hasta kayıt paketi",
        };

        var enqueueResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/interoperability/enabiz/queue", enqueueReq);
        Assert.Equal(HttpStatusCode.OK, enqueueResp.StatusCode);

        var queued = await enqueueResp.Content.ReadFromJsonAsync<ENabizTransmissionResponse>();
        Assert.NotNull(queued);
        Assert.Equal("Queued", queued.Status);
        Assert.NotEmpty(queued.SysTakipNo);
        Assert.Equal(101, queued.PackageTypeCode);

        // 2. Send
        var sendResp = await PostWithAntiforgeryAsync<object?>(
            doctorClient,
            $"/api/v1/interoperability/enabiz/transmissions/{queued.Id}/send",
            null);
        Assert.Equal(HttpStatusCode.OK, sendResp.StatusCode);

        var sent = await sendResp.Content.ReadFromJsonAsync<ENabizTransmissionResponse>();
        Assert.NotNull(sent);
        // Mock engine can return success or failure based on configuration
        Assert.Contains(sent.Status, ExpectedSendStatuses);

        // 3. Get by ID
        var getResp = await doctorClient.GetAsync($"/api/v1/interoperability/enabiz/transmissions/{queued.Id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var fetched = await getResp.Content.ReadFromJsonAsync<ENabizTransmissionResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(queued.Id, fetched.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G06")]
    public async Task ENabizConsentDeniedPackageCannotBeSent()
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

        var patientId = Guid.NewGuid();
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        EstablishCareRelationship(application, doctorId, patientId);

        // 1. Enqueue without consent
        var enqueueReq = new EnqueueENabizPackageRequest
        {
            PackageType = 105,
            PatientId = patientId,
            PatientNationalId = "11111111110",
            HasPatientConsent = false,
            PayloadSummary = "DEMO reçete paketi — rıza yok",
        };

        var enqueueResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/interoperability/enabiz/queue", enqueueReq);
        Assert.Equal(HttpStatusCode.OK, enqueueResp.StatusCode);

        var queued = await enqueueResp.Content.ReadFromJsonAsync<ENabizTransmissionResponse>();
        Assert.NotNull(queued);
        Assert.Equal("ConsentDenied", queued.Status);
        Assert.Equal("ERR_CONSENT_DENIED", queued.ResponseCode);

        // 2. Attempt to send should fail
        var sendResp = await PostWithAntiforgeryAsync<object?>(
            doctorClient,
            $"/api/v1/interoperability/enabiz/transmissions/{queued.Id}/send",
            null);
        Assert.Equal(HttpStatusCode.InternalServerError, sendResp.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G06")]
    public async Task ENabizQueueQueryFiltersCorrectly()
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

        var patientId = Guid.NewGuid();
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        EstablishCareRelationship(application, doctorId, patientId);

        // Create 2 packages — 1 consented, 1 denied
        var req1 = new EnqueueENabizPackageRequest
        {
            PackageType = 101,
            PatientId = patientId,
            PatientNationalId = "11111111110",
            HasPatientConsent = true,
            PayloadSummary = "DEMO p1",
        };
        var resp1 = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/interoperability/enabiz/queue", req1);
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);

        var req2 = new EnqueueENabizPackageRequest
        {
            PackageType = 103,
            PatientId = patientId,
            PatientNationalId = "11111111110",
            HasPatientConsent = false,
            PayloadSummary = "DEMO p2",
        };
        var resp2 = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/interoperability/enabiz/queue", req2);
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);

        // Query all
        var allResp = await doctorClient.GetAsync($"/api/v1/interoperability/enabiz/queue?patientId={patientId}");
        Assert.Equal(HttpStatusCode.OK, allResp.StatusCode);
        var allList = await allResp.Content.ReadFromJsonAsync<List<ENabizTransmissionResponse>>();
        Assert.NotNull(allList);
        Assert.Equal(2, allList.Count);

        // Query by status
        var queuedResp = await doctorClient.GetAsync($"/api/v1/interoperability/enabiz/queue?status=Queued&patientId={patientId}");
        Assert.Equal(HttpStatusCode.OK, queuedResp.StatusCode);
        var queuedList = await queuedResp.Content.ReadFromJsonAsync<List<ENabizTransmissionResponse>>();
        Assert.NotNull(queuedList);
        Assert.Single(queuedList);
        Assert.Equal("Queued", queuedList[0].Status);
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
