using System.Net;
using System.Net.Http.Json;

using HospitalManagement.Contracts.Diagnostics;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class SpecimenIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F06-G03")]
    public async Task SpecimenCollectTransitReceiveAndRejectLifecycleWithAudit()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);
        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(application);

        var doctorClient = CreateSecureClient(application);
        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");

        // 1. Doctor creates and places order
        var orderReq = new CreateDiagnosticOrderDraftRequest
        {
            PatientId = Guid.Parse("00000000-0000-0000-0000-000000000109"),
            EncounterId = encounterId,
            DepartmentId = Guid.Parse("30000000-0000-0000-0000-000000000003"),
            OrderType = "Laboratory",
            Priority = "Routine",
            ClinicalIndication = "Rutin Biyokimya Kontrolü",
            Items =
            [
                new CreateDiagnosticOrderItemRequest
                {
                    CatalogCode = "DEMO-LAB-GLU",
                    CatalogItemName = "Açlık Kan Şekeri (Glukoz)",
                    Category = "Klinik Biyokimya",
                },
            ],
        };

        var draftResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/diagnostics/orders", orderReq);
        Assert.Equal(HttpStatusCode.Created, draftResp.StatusCode);
        var order = await draftResp.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>();
        Assert.NotNull(order);

        var placeResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/diagnostics/orders/{order.Id}/place", new PlaceDiagnosticOrderRequest());
        Assert.Equal(HttpStatusCode.OK, placeResp.StatusCode);

        // 2. A nurse cannot enter the LAB-scoped specimen workflow.
        var nurseClient = CreateSecureClient(application);
        await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        var nurseDeniedResp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/diagnostics/specimens/collect", new CollectSpecimenRequest
        {
            DiagnosticOrderId = order.Id,
            PatientId = order.PatientId,
            SpecimenType = "Serum",
            ContainerType = "Sarı Kapaklı Jelli Tüp",
        });
        Assert.Equal(HttpStatusCode.Forbidden, nurseDeniedResp.StatusCode);

        var collectorClient = CreateSecureClient(application);
        await LoginAsync(collectorClient, "DEMO-labtech@hospital.invalid", "DEMO-LabTech-Pass!1");

        // Test wrong patient protection
        var wrongPatientReq = new CollectSpecimenRequest
        {
            DiagnosticOrderId = order.Id,
            PatientId = Guid.NewGuid(), // Different patient!
            SpecimenType = "Serum",
            ContainerType = "Sarı Kapaklı Jelli Tüp",
        };
        var wrongPatientResp = await PostWithAntiforgeryAsync(collectorClient, "/api/v1/diagnostics/specimens/collect", wrongPatientReq);
        Assert.Equal(HttpStatusCode.BadRequest, wrongPatientResp.StatusCode);

        // Valid collection
        var collectReq = new CollectSpecimenRequest
        {
            DiagnosticOrderId = order.Id,
            PatientId = order.PatientId,
            SpecimenType = "Serum",
            ContainerType = "Sarı Kapaklı Jelli Tüp",
            CollectionLocation = "Kan Alma Kabini 2",
            CollectionNotes = "Sorunsuz venöz kan alındı.",
        };

        var collectResp = await PostWithAntiforgeryAsync(collectorClient, "/api/v1/diagnostics/specimens/collect", collectReq);
        Assert.Equal(HttpStatusCode.Created, collectResp.StatusCode);
        var specimen = await collectResp.Content.ReadFromJsonAsync<SpecimenDetailResponse>();
        Assert.NotNull(specimen);
        Assert.StartsWith("DEMO-SMP-", specimen.Barcode, StringComparison.Ordinal);
        Assert.Equal("Collected", specimen.Status);
        Assert.Single(specimen.Transitions);

        await using (var inspectScope = application.Services.CreateAsyncScope())
        {
            var db = inspectScope.ServiceProvider.GetRequiredService<DiagnosticsDbContext>();
            var dbSpecimen = await db.Specimens.Include(x => x.Transitions).FirstOrDefaultAsync(x => x.Id == specimen.Id);
            Assert.NotNull(dbSpecimen);
            Assert.Equal("Collected", dbSpecimen.Status.ToString());
            Assert.Equal(1, dbSpecimen.Version);
            Assert.Single(dbSpecimen.Transitions);
        }

        // 3. Authorized LAB collector transits specimen
        var transitResp = await PostWithAntiforgeryAsync(collectorClient, $"/api/v1/diagnostics/specimens/{specimen.Id}/transit", new TransitSpecimenRequest
        {
            Location = "Pnömatik Taşıma İstasyonu",
            Notes = "Tüp kapsüle yerleştirildi.",
        });
        Assert.Equal(HttpStatusCode.OK, transitResp.StatusCode);

        // 4. Lab Tech receives specimen
        var techClient = CreateSecureClient(application);
        await LoginAsync(techClient, "DEMO-labtech@hospital.invalid", "DEMO-LabTech-Pass!1");

        var receiveResp = await PostWithAntiforgeryAsync(techClient, $"/api/v1/diagnostics/specimens/{specimen.Id}/receive", new ReceiveSpecimenRequest
        {
            Location = "Merkez Laboratuvar Numune Masası",
            Notes = "Kabul edildi.",
        });
        Assert.Equal(HttpStatusCode.OK, receiveResp.StatusCode);

        // 5. Lookup by barcode
        var barcodeResp = await techClient.GetAsync($"/api/v1/diagnostics/specimens/by-barcode/{specimen.Barcode}");
        Assert.Equal(HttpStatusCode.OK, barcodeResp.StatusCode);
        var lookedUp = await barcodeResp.Content.ReadFromJsonAsync<SpecimenDetailResponse>();
        Assert.NotNull(lookedUp);
        Assert.Equal("Received", lookedUp.Status);
        Assert.Equal(3, lookedUp.Transitions.Count);

        // 6. Test specimen rejection on a second specimen
        var collectReq2 = new CollectSpecimenRequest
        {
            DiagnosticOrderId = order.Id,
            PatientId = order.PatientId,
            SpecimenType = "Venöz Tam Kan",
            ContainerType = "Mor Kapaklı EDTA Tüp",
        };
        var collectResp2 = await PostWithAntiforgeryAsync(collectorClient, "/api/v1/diagnostics/specimens/collect", collectReq2);
        Assert.Equal(HttpStatusCode.Created, collectResp2.StatusCode);
        var specimen2 = await collectResp2.Content.ReadFromJsonAsync<SpecimenDetailResponse>();
        Assert.NotNull(specimen2);

        var rejectResp = await PostWithAntiforgeryAsync(techClient, $"/api/v1/diagnostics/specimens/{specimen2.Id}/reject", new RejectSpecimenRequest
        {
            RejectionReason = "Hemolizli numune (Analize uygun değil)",
            Notes = "Numune kırmızı renkli ve hemolizli geldi, yeniden kan istenmeli.",
        });
        Assert.Equal(HttpStatusCode.OK, rejectResp.StatusCode);
        var rejectedSpecimen = await rejectResp.Content.ReadFromJsonAsync<SpecimenDetailResponse>();
        Assert.NotNull(rejectedSpecimen);
        Assert.Equal("Rejected", rejectedSpecimen.Status);
        Assert.Equal("Hemolizli numune (Analize uygun değil)", rejectedSpecimen.RejectionReason);

        // 7. Verify Audit Logs
        await using var scope = application.Services.CreateAsyncScope();
        var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var logs = await auditDb.AuditLogs.AsNoTracking().ToListAsync();
        Assert.Contains(logs, l => l.Action == "Diagnostics.SpecimenCollect");
        Assert.Contains(logs, l => l.Action == "Diagnostics.SpecimenTransit");
        Assert.Contains(logs, l => l.Action == "Diagnostics.SpecimenReceive");
        Assert.Contains(logs, l => l.Action == "Diagnostics.SpecimenReject");
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

        var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
        await identitySeeder.SeedAsync();

        var labSeeder = scope.ServiceProvider.GetRequiredService<ILabCatalogDataSeeder>();
        await labSeeder.SeedAsync();
    }
}
