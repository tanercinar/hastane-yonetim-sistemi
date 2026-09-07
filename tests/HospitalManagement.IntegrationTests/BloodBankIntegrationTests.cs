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

public sealed class BloodBankIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F06-G09")]
    public async Task CompleteBloodBankWorkflowFromOrderToCrossmatchIssueAndTransfusion()
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

        // 1. Doctor creates and places order for Blood Bank
        var orderDraftReq = new CreateDiagnosticOrderDraftRequest
        {
            PatientId = Guid.Parse("00000000-0000-0000-0000-000000000109"),
            EncounterId = encounterId,
            DepartmentId = Guid.Parse("30000000-0000-0000-0000-000000000003"),
            OrderType = "BloodBank",
            Priority = "Urgent",
            ClinicalIndication = "Ameliyat öncesi 1 Ünite ES hazırlığı",
            Items =
            [
                new() { CatalogCode = "DEMO-BB-RBC-PREP", CatalogItemName = "Eritrosit Süspansiyonu Hazırlığı ve Crossmatch" },
            ],
        };

        var orderResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/diagnostics/orders", orderDraftReq);
        Assert.Equal(HttpStatusCode.Created, orderResp.StatusCode);
        var order = await orderResp.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>();
        Assert.NotNull(order);

        await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/diagnostics/orders/{order.Id}/place", new PlaceDiagnosticOrderRequest());

        // 2. Create Crossmatch Request for Patient (B Positive)
        var crossmatchReq = new CreateCrossmatchRequestDto
        {
            DiagnosticOrderId = order.Id,
            DiagnosticOrderItemId = order.Items[0].Id,
            PatientId = order.PatientId,
            PatientBloodGroup = "BPositive",
            RequestedProductType = "RedBloodCells",
            UnitsRequested = 1,
            RequiredByUtc = DateTime.UtcNow.AddHours(4),
        };

        var cmCreateResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/diagnostics/blood-bank/crossmatch", crossmatchReq);
        Assert.Equal(HttpStatusCode.Created, cmCreateResp.StatusCode);
        var crossmatch = await cmCreateResp.Content.ReadFromJsonAsync<CrossmatchDetailResponse>();
        Assert.NotNull(crossmatch);
        Assert.Equal("Requested", crossmatch.Status);

        // 3. Lab Technician logs in and queries Inventory
        var labClient = CreateSecureClient(application);
        await LoginAsync(labClient, "DEMO-labtech@hospital.invalid", "DEMO-LabTech-Pass!1");

        var inventoryResp = await labClient.GetAsync("/api/v1/diagnostics/blood-bank/inventory");
        Assert.Equal(HttpStatusCode.OK, inventoryResp.StatusCode);
        var inventory = await inventoryResp.Content.ReadFromJsonAsync<List<BloodUnitResponse>>();
        Assert.NotNull(inventory);
        Assert.NotEmpty(inventory);

        var aPosUnit = inventory.First(u => u.BloodGroup == "APositive" && u.ProductType == "RedBloodCells");
        var oNegUnit = inventory.First(u => u.BloodGroup == "ONegative" && u.ProductType == "RedBloodCells");

        // 4. Incompatible Crossmatch Test (A+ RBC to B+ Patient) -> Deterministik Red
        var incompTestReq = new PerformCrossmatchRequestDto
        {
            BloodUnitId = aPosUnit.Id,
            TechnicianNotes = "A+ kan ünitesi ile aglütinasyon pozitif.",
        };
        var incompResp = await PostWithAntiforgeryAsync(labClient, $"/api/v1/diagnostics/blood-bank/crossmatch/{crossmatch.Id}/test", incompTestReq);
        Assert.Equal(HttpStatusCode.BadRequest, incompResp.StatusCode);

        // 5. Compatible Crossmatch Test (O- RBC to B+ Patient) -> Deterministik Uygun
        var compTestReq = new PerformCrossmatchRequestDto
        {
            BloodUnitId = oNegUnit.Id,
            TechnicianNotes = "0- kan ünitesi ile tam uyumluluk sağlandı.",
        };
        var compResp = await PostWithAntiforgeryAsync(labClient, $"/api/v1/diagnostics/blood-bank/crossmatch/{crossmatch.Id}/test", compTestReq);
        Assert.Equal(HttpStatusCode.OK, compResp.StatusCode);
        var compResult = await compResp.Content.ReadFromJsonAsync<CrossmatchDetailResponse>();
        Assert.NotNull(compResult);
        Assert.Equal("Completed", compResult.Status);
        Assert.Equal("Compatible", compResult.CompatibilityResult);
        Assert.Equal(oNegUnit.Id, compResult.AllocatedBloodUnitId);

        // 6. Issue Blood Unit to clinical ward
        var issueResp = await PostWithAntiforgeryAsync(labClient, $"/api/v1/diagnostics/blood-bank/units/{oNegUnit.Id}/issue", new
        {
        });
        Assert.Equal(HttpStatusCode.OK, issueResp.StatusCode);
        var issuedUnit = await issueResp.Content.ReadFromJsonAsync<BloodUnitResponse>();
        Assert.NotNull(issuedUnit);
        Assert.Equal("Issued", issuedUnit.Status);

        // 7. Record Transfusion
        var transfuseResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/diagnostics/blood-bank/units/{oNegUnit.Id}/transfuse", new RecordTransfusionRequestDto { TransfusionNotes = "Reaksiyonsuz transfüzyon tamamlandı." });
        Assert.Equal(HttpStatusCode.OK, transfuseResp.StatusCode);
        var transfusedUnit = await transfuseResp.Content.ReadFromJsonAsync<BloodUnitResponse>();
        Assert.NotNull(transfusedUnit);
        Assert.Equal("Transfused", transfusedUnit.Status);

        // 8. Patient IDOR check
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var patientCmResp = await patientClient.GetAsync($"/api/v1/diagnostics/blood-bank/crossmatch/{crossmatch.Id}");
        Assert.Equal(HttpStatusCode.OK, patientCmResp.StatusCode);

        // 9. Verify Audit Logs
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var audits = await auditDb.AuditLogs
                .Where(a => a.TargetResourceType == "CrossmatchRequest" || a.TargetResourceType == "BloodUnit")
                .ToListAsync();

            Assert.Contains(audits, a => a.Action == "Diagnostics.BloodBankCrossmatchRequest");
            Assert.Contains(audits, a => a.Action == "Diagnostics.BloodBankCrossmatchIncompatible");
            Assert.Contains(audits, a => a.Action == "Diagnostics.BloodBankCrossmatchCompatible");
            Assert.Contains(audits, a => a.Action == "Diagnostics.BloodBankUnitIssue");
            Assert.Contains(audits, a => a.Action == "Diagnostics.BloodBankTransfusionRecord");
        }
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

        var radSeeder = scope.ServiceProvider.GetRequiredService<IRadiologyCatalogDataSeeder>();
        await radSeeder.SeedAsync();

        var bloodSeeder = scope.ServiceProvider.GetRequiredService<IBloodBankDataSeeder>();
        await bloodSeeder.SeedAsync();
    }
}
