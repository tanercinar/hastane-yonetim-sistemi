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

public sealed class Hl7V2IntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G03")]
    public async Task Hl7InboundValidMessageReturnsAckAA()
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

        var validEr7 = "MSH|^~\\&|LAB_LIS|LAB_FACILITY|HMS_CORE|HMS_HOSPITAL|20260901120000||ORU^R01|MSG-INTEG-1001|P|2.3.1\rPID|1||DEMO-PAT-123\rOBR|1|ORD-1\rOBX|1|NM|GLU||95|mg/dL|N|||F";

        var request = new Hl7InboundRequest
        {
            RawEr7Message = validEr7,
        };

        var response = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/interoperability/hl7/inbound", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ack = await response.Content.ReadFromJsonAsync<Hl7InboundResponse>();
        Assert.NotNull(ack);
        Assert.Equal("AA", ack.AckCode);
        Assert.StartsWith("ACK-", ack.MessageControlId, StringComparison.Ordinal);
        Assert.Contains("MSA|AA|MSG-INTEG-1001", ack.RawEr7Content, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G03")]
    public async Task Hl7InboundCorruptMessageReturnsAckAEAndEntersDeadLetterQueue()
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

        var corruptEr7 = "CORRUPT_HEADER_WITHOUT_MSH_FIELDS";

        var request = new Hl7InboundRequest
        {
            RawEr7Message = corruptEr7,
        };

        // 1. Send corrupt message -> Expect AE ACK
        var response = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/interoperability/hl7/inbound", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ack = await response.Content.ReadFromJsonAsync<Hl7InboundResponse>();
        Assert.NotNull(ack);
        Assert.Equal("AE", ack.AckCode);

        // 2. Query Dead-Letter Queue
        var dlResp = await doctorClient.GetAsync("/api/v1/interoperability/hl7/dead-letter");
        Assert.Equal(HttpStatusCode.OK, dlResp.StatusCode);
        var dlEntries = await dlResp.Content.ReadFromJsonAsync<List<Hl7DeadLetterResponse>>();
        Assert.NotNull(dlEntries);
        Assert.NotEmpty(dlEntries);
        var entry = dlEntries[0];
        Assert.False(entry.IsResolved);

        // 3. Retry Dead-Letter Entry
        var retryResp = await PostWithAntiforgeryAsync<object?>(doctorClient, $"/api/v1/interoperability/hl7/dead-letter/{entry.Id}/retry", null);
        Assert.Equal(HttpStatusCode.NoContent, retryResp.StatusCode);

        // 4. Verify resolved
        var dlResp2 = await doctorClient.GetAsync("/api/v1/interoperability/hl7/dead-letter");
        var dlEntries2 = await dlResp2.Content.ReadFromJsonAsync<List<Hl7DeadLetterResponse>>();
        Assert.NotNull(dlEntries2);
        var updatedEntry = dlEntries2.First(e => e.Id == entry.Id);
        Assert.True(updatedEntry.IsResolved);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G03")]
    public async Task Hl7GenerateMessageProducesEr7Text()
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

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        EstablishCareRelationship(application, doctorId, patientId);

        var genReq = new Hl7GenerateRequest
        {
            PatientId = patientId,
            ProtocolNumber = "PROTO-GEN-999",
            WardName = "Genel Cerrahi",
            BedNumber = "GC-301",
        };

        var response = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/interoperability/hl7/generate/ADT_A01", genReq);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var genResult = await response.Content.ReadFromJsonAsync<Hl7GenerateResponse>();
        Assert.NotNull(genResult);
        Assert.Equal("ADT^A01", genResult.MessageType);
        Assert.Contains("MSH|^~\\&|HMS_CORE", genResult.RawEr7Message, StringComparison.Ordinal);
        Assert.Contains("PV1|1|I|Genel Cerrahi^GC-301^BED", genResult.RawEr7Message, StringComparison.Ordinal);
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
