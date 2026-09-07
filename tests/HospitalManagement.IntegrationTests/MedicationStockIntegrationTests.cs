using System.Net;
using System.Net.Http.Json;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Pharmacy;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Application;
using HospitalManagement.Modules.Pharmacy.Domain;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class MedicationStockIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G06")]
    public async Task PharmacistCanQueryStockOverviewAndFefoCandidates()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var pharmacistClient = CreateSecureClient(application);
        var loginResp = await LoginAsync(pharmacistClient, "DEMO-pharmacist@hospital.invalid", "DEMO-Pharm-Pass!1");
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        // 1. Query stock overview
        var stockResp = await pharmacistClient.GetAsync("/api/v1/pharmacy/inventory/stock");
        Assert.Equal(HttpStatusCode.OK, stockResp.StatusCode);
        var stockList = await stockResp.Content.ReadFromJsonAsync<List<MedicationStockItemResponse>>();
        Assert.NotNull(stockList);
        Assert.NotEmpty(stockList);
        Assert.Contains(stockList, s => s.MedicationCode == "DEMO-MED-AMX500");

        // 2. Query FEFO candidates for Amoxicillin
        var amoxItem = stockList.First(s => s.MedicationCode == "DEMO-MED-AMX500");
        var fefoResp = await pharmacistClient.GetAsync($"/api/v1/pharmacy/inventory/fefo-candidates/{amoxItem.MedicationCatalogItemId}");
        Assert.Equal(HttpStatusCode.OK, fefoResp.StatusCode);
        var fefoList = await fefoResp.Content.ReadFromJsonAsync<List<FefoCandidateStockResponse>>();
        Assert.NotNull(fefoList);
        Assert.Equal(2, fefoList.Count);

        // FEFO order: first candidate expires sooner than second candidate
        Assert.True(fefoList[0].ExpirationDateUtc <= fefoList[1].ExpirationDateUtc);
        Assert.True(fefoList[0].DaysUntilExpiration <= fefoList[1].DaysUntilExpiration);

        // 3. Verify audit log
        await using var scope = application.Services.CreateAsyncScope();
        var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var logs = await auditDb.AuditLogs.AsNoTracking().ToListAsync();
        Assert.Contains(logs, l => l.Action == "Pharmacy.StockView");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G06")]
    public async Task PharmacistCanAdjustStockWithReasonAndTransactionAudit()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var pharmacistClient = CreateSecureClient(application);
        var loginResp = await LoginAsync(pharmacistClient, "DEMO-pharmacist@hospital.invalid", "DEMO-Pharm-Pass!1");
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        // 1. Pick a stock item
        var stockResp = await pharmacistClient.GetAsync("/api/v1/pharmacy/inventory/stock");
        var stockList = await stockResp.Content.ReadFromJsonAsync<List<MedicationStockItemResponse>>();
        Assert.NotNull(stockList);
        var target = stockList.First();

        // 2. Adjust stock
        var adjustReq = new AdjustStockRequest
        {
            StockItemId = target.Id,
            ExpectedVersion = target.Version,
            NewQuantity = target.QuantityOnHand + 25,
            Reason = "Yıllık fiziksel sayım fazlası tespit edildi",
        };

        var adjustResp = await PostWithAntiforgeryAsync(pharmacistClient, "/api/v1/pharmacy/inventory/adjust", adjustReq);
        Assert.Equal(HttpStatusCode.OK, adjustResp.StatusCode);
        var updated = await adjustResp.Content.ReadFromJsonAsync<MedicationStockItemResponse>();
        Assert.NotNull(updated);
        Assert.Equal(target.QuantityOnHand + 25, updated.QuantityOnHand);

        var staleAdjustment = new AdjustStockRequest
        {
            StockItemId = target.Id,
            ExpectedVersion = target.Version,
            NewQuantity = target.QuantityOnHand + 30,
            Reason = "Eski ekran üzerinden yinelenen sayım düzeltmesi",
        };
        var staleResponse = await PostWithAntiforgeryAsync(
            pharmacistClient,
            "/api/v1/pharmacy/inventory/adjust",
            staleAdjustment);
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);

        // 3. Verify transaction record
        var txResp = await pharmacistClient.GetAsync($"/api/v1/pharmacy/inventory/stock/{target.Id}/transactions");
        Assert.Equal(HttpStatusCode.OK, txResp.StatusCode);
        var txs = await txResp.Content.ReadFromJsonAsync<List<MedicationStockTransactionResponse>>();
        Assert.NotNull(txs);
        Assert.Contains(txs, t => t.TransactionType == "AdjustmentIn" && t.Quantity == 25);

        // 4. Verify audit log
        await using var scope = application.Services.CreateAsyncScope();
        var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var logs = await auditDb.AuditLogs.AsNoTracking().ToListAsync();
        Assert.Contains(logs, l => l.Action == "Pharmacy.StockAdjustment" && l.TargetResourceId == target.Id.ToString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G06")]
    public async Task StockAdjustmentInvalidInputReturnsValidationProblem()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var pharmacistClient = CreateSecureClient(application);
        await LoginAsync(pharmacistClient, "DEMO-pharmacist@hospital.invalid", "DEMO-Pharm-Pass!1");

        // Negative quantity
        var req1 = new AdjustStockRequest
        {
            StockItemId = Guid.NewGuid(),
            NewQuantity = -5,
            Reason = "Geçersiz stok",
        };
        var resp1 = await PostWithAntiforgeryAsync(pharmacistClient, "/api/v1/pharmacy/inventory/adjust", req1);
        Assert.Equal(HttpStatusCode.BadRequest, resp1.StatusCode);

        // Reason too short
        var req2 = new AdjustStockRequest
        {
            StockItemId = Guid.NewGuid(),
            NewQuantity = 10,
            Reason = "kısa",
        };
        var resp2 = await PostWithAntiforgeryAsync(pharmacistClient, "/api/v1/pharmacy/inventory/adjust", req2);
        Assert.Equal(HttpStatusCode.BadRequest, resp2.StatusCode);
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

        var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
        await identitySeeder.SeedAsync();

        var organizationSeeder = scope.ServiceProvider.GetRequiredService<IOrganizationDataSeeder>();
        await organizationSeeder.SeedAsync();

        var pharmacySeeder = scope.ServiceProvider.GetRequiredService<IMedicationCatalogDataSeeder>();
        await pharmacySeeder.SeedAsync();
    }
}
