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

public sealed class MockServerIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G01")]
    public async Task MockServerConfigCanBeRetrievedAndUpdated()
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

        // 1. Get configs
        var configsResp = await adminClient.GetAsync("/api/v1/interoperability/mock-engine/configs");
        Assert.Equal(HttpStatusCode.OK, configsResp.StatusCode);
        var configs = await configsResp.Content.ReadFromJsonAsync<List<MockServerConfigResponse>>();
        Assert.NotNull(configs);
        Assert.NotEmpty(configs);

        // 2. Update config for Fhir
        var updateReq = new UpdateMockServerConfigRequest
        {
            SystemType = "Fhir",
            IsEnabled = true,
            FaultMode = "Latency",
            LatencyMilliseconds = 150,
            FailureRatePercentage = 0,
            MaxRetryAttempts = 2,
            TimeoutSeconds = 15,
        };

        var updateResp = await PutWithAntiforgeryAsync(adminClient, "/api/v1/interoperability/mock-engine/configs/Fhir", updateReq);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updated = await updateResp.Content.ReadFromJsonAsync<MockServerConfigResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Fhir", updated.SystemType);
        Assert.Equal("Latency", updated.FaultMode);
        Assert.Equal(150, updated.LatencyMilliseconds);

        var patientClient = CreateSecureClient(application);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await patientClient.GetAsync("/api/v1/interoperability/mock-engine/configs")).StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G01")]
    public async Task MockEngineSimulateOperationExecutesAndLogs()
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

        // 1. Simulate HL7 Operation
        var simReq = new SimulateMockEngineRequest
        {
            SystemType = "Hl7V2",
            ActionName = "SendAdtA01",
            Payload = "{\"messageType\": \"ADT^A01\"}",
        };

        var simResp = await PostWithAntiforgeryAsync(adminClient, "/api/v1/interoperability/mock-engine/simulate", simReq);
        Assert.Equal(HttpStatusCode.OK, simResp.StatusCode);
        var simResult = await simResp.Content.ReadFromJsonAsync<SimulateMockEngineResponse>();
        Assert.NotNull(simResult);
        Assert.True(simResult.IsSuccess);
        Assert.NotEmpty(simResult.CorrelationId);

        // 2. Query Logs
        var logsResp = await adminClient.GetAsync("/api/v1/interoperability/mock-engine/logs?systemType=Hl7V2");
        Assert.Equal(HttpStatusCode.OK, logsResp.StatusCode);
        var logs = await logsResp.Content.ReadFromJsonAsync<List<IntegrationMessageLogResponse>>();
        Assert.NotNull(logs);
        Assert.Contains(logs, l => l.ActionName == "SendAdtA01" && l.Status == "Success");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G01")]
    public async Task MockEngineFaultModeOfflineFailsOperationAndCircuitResetWorks()
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

        // 1. Set Mhrs FaultMode to Offline
        var updateReq = new UpdateMockServerConfigRequest
        {
            SystemType = "Mhrs",
            IsEnabled = true,
            FaultMode = "Offline",
            LatencyMilliseconds = 0,
            FailureRatePercentage = 0,
            MaxRetryAttempts = 0,
            TimeoutSeconds = 5,
        };
        var updateResp = await PutWithAntiforgeryAsync(adminClient, "/api/v1/interoperability/mock-engine/configs/Mhrs", updateReq);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        // 2. Simulate Operation -> Should Fail
        var simReq = new SimulateMockEngineRequest
        {
            SystemType = "Mhrs",
            ActionName = "QuerySlots",
            Payload = "{}",
        };
        var simResp = await PostWithAntiforgeryAsync(adminClient, "/api/v1/interoperability/mock-engine/simulate", simReq);
        Assert.Equal(HttpStatusCode.OK, simResp.StatusCode);
        var simResult = await simResp.Content.ReadFromJsonAsync<SimulateMockEngineResponse>();
        Assert.NotNull(simResult);
        Assert.False(simResult.IsSuccess);
        Assert.Contains("Offline", simResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // 3. Reset Circuit Breaker
        var resetResp = await PostWithAntiforgeryAsync<object?>(adminClient, "/api/v1/interoperability/mock-engine/reset-circuit/Mhrs", null);
        Assert.Equal(HttpStatusCode.NoContent, resetResp.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-KAPI")]
    public async Task MockEngineEnforcesTimeoutAndDoesNotPersistCallerPayload()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        using var application = new ApiWebApplicationFactory(database.ConnectionString);
        await RunAllMigrationsAndSeedAsync(application);

        var adminClient = CreateSecureClient(application);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1")).StatusCode);

        var updateResponse = await PutWithAntiforgeryAsync(
            adminClient,
            "/api/v1/interoperability/mock-engine/configs/Fhir",
            new UpdateMockServerConfigRequest
            {
                SystemType = "Fhir",
                IsEnabled = true,
                FaultMode = "Latency",
                LatencyMilliseconds = 1500,
                FailureRatePercentage = 0,
                MaxRetryAttempts = 0,
                TimeoutSeconds = 1,
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        const string canary = "DEMO-SENSITIVE-PAYLOAD-MUST-NOT-BE-LOGGED";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var simulateResponse = await PostWithAntiforgeryAsync(
            adminClient,
            "/api/v1/interoperability/mock-engine/simulate",
            new SimulateMockEngineRequest
            {
                SystemType = "Fhir",
                ActionName = "TimeoutProbe",
                Payload = canary,
            });
        stopwatch.Stop();

        Assert.Equal(HttpStatusCode.OK, simulateResponse.StatusCode);
        var result = await simulateResponse.Content.ReadFromJsonAsync<SimulateMockEngineResponse>();
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal("MOCK_INTEGRATION_TIMEOUT", result.ErrorMessage);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(1400), $"Timeout took {stopwatch.Elapsed}.");

        var logs = await adminClient.GetFromJsonAsync<List<IntegrationMessageLogResponse>>(
            "/api/v1/interoperability/mock-engine/logs?systemType=Fhir");
        var log = Assert.Single(logs!, candidate => candidate.ActionName == "TimeoutProbe");
        Assert.Equal("MOCK_INTEGRATION_TIMEOUT", log.ErrorMessage);
        Assert.DoesNotContain(canary, log.PayloadSummary, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-KAPI")]
    public async Task MockEngineCorruptPayloadFaultFailsSafelyWithoutLeakingPayload()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        using var application = new ApiWebApplicationFactory(database.ConnectionString);
        await RunAllMigrationsAndSeedAsync(application);

        var adminClient = CreateSecureClient(application);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1")).StatusCode);

        var updateResponse = await PutWithAntiforgeryAsync(
            adminClient,
            "/api/v1/interoperability/mock-engine/configs/Hl7V2",
            new UpdateMockServerConfigRequest
            {
                SystemType = "Hl7V2",
                IsEnabled = true,
                FaultMode = "CorruptPayload",
                LatencyMilliseconds = 0,
                FailureRatePercentage = 0,
                MaxRetryAttempts = 3,
                TimeoutSeconds = 5,
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        const string canary = "DEMO-CORRUPT-CLINICAL-CONTENT-MUST-NOT-LEAK";
        var simulateResponse = await PostWithAntiforgeryAsync(
            adminClient,
            "/api/v1/interoperability/mock-engine/simulate",
            new SimulateMockEngineRequest
            {
                SystemType = "Hl7V2",
                ActionName = "CorruptPayloadProbe",
                Payload = canary,
            });

        Assert.Equal(HttpStatusCode.OK, simulateResponse.StatusCode);
        var result = await simulateResponse.Content.ReadFromJsonAsync<SimulateMockEngineResponse>();
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal("MOCK_INTEGRATION_CORRUPT_PAYLOAD", result.ErrorMessage);
        Assert.Equal(0, result.RetryAttempts);

        var logs = await adminClient.GetFromJsonAsync<List<IntegrationMessageLogResponse>>(
            "/api/v1/interoperability/mock-engine/logs?systemType=Hl7V2");
        var log = Assert.Single(logs!, candidate => candidate.ActionName == "CorruptPayloadProbe");
        Assert.Equal("MOCK_INTEGRATION_CORRUPT_PAYLOAD", log.ErrorMessage);
        Assert.DoesNotContain(canary, log.PayloadSummary, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-KAPI")]
    public async Task MockEngineRetriesTransientFailureOnceAndWritesSingleTechnicalLog()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        using var application = new ApiWebApplicationFactory(database.ConnectionString);
        await RunAllMigrationsAndSeedAsync(application);

        var adminClient = CreateSecureClient(application);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1")).StatusCode);

        var updateResponse = await PutWithAntiforgeryAsync(
            adminClient,
            "/api/v1/interoperability/mock-engine/configs/DicomPacs",
            new UpdateMockServerConfigRequest
            {
                SystemType = "DicomPacs",
                IsEnabled = true,
                FaultMode = "TransientError",
                LatencyMilliseconds = 0,
                FailureRatePercentage = 0,
                MaxRetryAttempts = 2,
                TimeoutSeconds = 5,
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var simulateResponse = await PostWithAntiforgeryAsync(
            adminClient,
            "/api/v1/interoperability/mock-engine/simulate",
            new SimulateMockEngineRequest
            {
                SystemType = "DicomPacs",
                ActionName = "RetryProbe",
                Payload = "DEMO-RETRY-PAYLOAD",
            });

        Assert.Equal(HttpStatusCode.OK, simulateResponse.StatusCode);
        var result = await simulateResponse.Content.ReadFromJsonAsync<SimulateMockEngineResponse>();
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.RetryAttempts);

        var logs = await adminClient.GetFromJsonAsync<List<IntegrationMessageLogResponse>>(
            "/api/v1/interoperability/mock-engine/logs?systemType=DicomPacs");
        var log = Assert.Single(logs!, candidate => candidate.ActionName == "RetryProbe");
        Assert.Equal("Retried", log.Status);
        Assert.Equal(1, log.RetryCount);
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
}
