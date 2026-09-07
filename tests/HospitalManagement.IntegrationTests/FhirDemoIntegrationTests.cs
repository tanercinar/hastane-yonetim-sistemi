using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Interoperability;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using HospitalManagement.Modules.Interoperability.Domain.Fhir;
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

public sealed class FhirDemoIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G02")]
    public async Task FhirCapabilityStatementReturnsValidMetadata()
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

        var response = await doctorClient.GetAsync("/api/v1/interoperability/fhir/r4/metadata");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var capability = await response.Content.ReadFromJsonAsync<FhirCapabilityStatement>();
        Assert.NotNull(capability);
        Assert.Equal("CapabilityStatement", capability.ResourceType);
        Assert.Equal("4.0.1", capability.FhirVersion);
        Assert.Contains("json", capability.Format);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G02")]
    public async Task FhirPatientAndPractitionerCanBeRetrieved()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var chiefClient = CreateSecureClient(application);
        var chiefLogin = await LoginAsync(chiefClient, "DEMO-chief@hospital.invalid", "DEMO-Chief-Pass!1");
        Assert.Equal(HttpStatusCode.OK, chiefLogin.StatusCode);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        EstablishCareRelationship(application, Guid.Parse("00000000-0000-0000-0000-000000000108"), patientId);

        // 1. Get Patient
        var patResp = await chiefClient.GetAsync($"/api/v1/interoperability/fhir/r4/Patient/{patientId}");
        Assert.Equal(HttpStatusCode.OK, patResp.StatusCode);
        var patient = await patResp.Content.ReadFromJsonAsync<FhirPatient>();
        Assert.NotNull(patient);
        Assert.Equal("Patient", patient.ResourceType);
        Assert.Equal(patientId.ToString(), patient.Id);

        // 2. Get Practitioner
        var pracResp = await chiefClient.GetAsync($"/api/v1/interoperability/fhir/r4/Practitioner/{doctorId}");
        Assert.Equal(HttpStatusCode.OK, pracResp.StatusCode);
        var prac = await pracResp.Content.ReadFromJsonAsync<FhirPractitioner>();
        Assert.NotNull(prac);
        Assert.Equal("Practitioner", prac.ResourceType);
        Assert.Equal(doctorId.ToString(), prac.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G02")]
    public async Task FhirPatientExportReturnsCompleteBundle()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var chiefClient = CreateSecureClient(application);
        var chiefLogin = await LoginAsync(chiefClient, "DEMO-chief@hospital.invalid", "DEMO-Chief-Pass!1");
        Assert.Equal(HttpStatusCode.OK, chiefLogin.StatusCode);

        var patientClient = CreateSecureClient(application);
        var patientLogin = await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");
        Assert.Equal(HttpStatusCode.OK, patientLogin.StatusCode);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        EstablishCareRelationship(application, Guid.Parse("00000000-0000-0000-0000-000000000108"), patientId);

        var deniedResponse = await patientClient.GetAsync($"/api/v1/interoperability/fhir/r4/Patient/{patientId}/$export");
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);

        using var anonymousClient = CreateSecureClient(application);
        var anonymousResponse = await anonymousClient.GetAsync($"/api/v1/interoperability/fhir/r4/Patient/{patientId}/$export");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var exportResp = await chiefClient.GetAsync($"/api/v1/interoperability/fhir/r4/Patient/{patientId}/$export");
        Assert.Equal(HttpStatusCode.OK, exportResp.StatusCode);

        var bundle = await exportResp.Content.ReadFromJsonAsync<FhirBundle>();
        Assert.NotNull(bundle);
        Assert.Equal("Bundle", bundle.ResourceType);
        Assert.Equal("collection", bundle.Type);
        Assert.True(bundle.Total >= 4);
        Assert.NotEmpty(bundle.Entry);

        var adminClient = CreateSecureClient(application);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1")).StatusCode);
        var configResponse = await PutWithAntiforgeryAsync(
            adminClient,
            "/api/v1/interoperability/mock-engine/configs/Fhir",
            new UpdateMockServerConfigRequest
            {
                SystemType = "Fhir",
                IsEnabled = true,
                FaultMode = "Offline",
                LatencyMilliseconds = 0,
                FailureRatePercentage = 0,
                MaxRetryAttempts = 0,
                TimeoutSeconds = 5,
            });
        Assert.Equal(HttpStatusCode.OK, configResponse.StatusCode);

        var unavailableResponse = await chiefClient.GetAsync(
            $"/api/v1/interoperability/fhir/r4/Patient/{patientId}/$export");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailableResponse.StatusCode);
        var unavailableBody = await unavailableResponse.Content.ReadAsStringAsync();
        Assert.Contains("sunucu", unavailableBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", unavailableBody, StringComparison.Ordinal);
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

    private static async Task<HttpResponseMessage> PutWithAntiforgeryAsync<T>(
        HttpClient client,
        string url,
        T body)
    {
        var antiforgery = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/v1/identity/antiforgery");
        Assert.NotNull(antiforgery);

        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("X-HMS-CSRF", antiforgery.Token);
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
