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

public sealed class LabCatalogIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F06-G02")]
    public async Task SeedingAndQueryingLabCatalogSupportsSearchAndParameters()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");

        // 1. Query all catalog items
        var allResp = await doctorClient.GetAsync("/api/v1/diagnostics/lab-catalog?maxResults=50");
        Assert.Equal(HttpStatusCode.OK, allResp.StatusCode);
        var allItems = await allResp.Content.ReadFromJsonAsync<List<LabCatalogSummaryResponse>>();
        Assert.NotNull(allItems);
        Assert.True(allItems.Count >= 10);

        // 2. Search by query "Hemogram"
        var searchResp = await doctorClient.GetAsync("/api/v1/diagnostics/lab-catalog?query=Hemogram");
        Assert.Equal(HttpStatusCode.OK, searchResp.StatusCode);
        var searchResults = await searchResp.Content.ReadFromJsonAsync<List<LabCatalogSummaryResponse>>();
        Assert.NotNull(searchResults);
        Assert.Contains(searchResults, i => i.Code == "DEMO-LAB-CBC");

        // 3. Get CBC item detail with parameters
        var cbcSummary = searchResults.First(i => i.Code == "DEMO-LAB-CBC");
        var detailResp = await doctorClient.GetAsync($"/api/v1/diagnostics/lab-catalog/{cbcSummary.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResp.StatusCode);
        var cbcDetail = await detailResp.Content.ReadFromJsonAsync<LabCatalogItemResponse>();
        Assert.NotNull(cbcDetail);
        Assert.Equal("DEMO-LAB-CBC", cbcDetail.Code);
        Assert.True(cbcDetail.IsPanel);
        Assert.Contains(cbcDetail.Parameters, p => p.Code == "WBC");
        Assert.Contains(cbcDetail.Parameters, p => p.Code == "HGB");
        Assert.Contains(cbcDetail.Parameters, p => p.Code == "PLT");

        // 4. Test versioned import
        var importReq = new ImportLabCatalogRequest
        {
            CatalogVersion = "DEMO-LAB-2026.2-CUSTOM",
            Items =
            [
                new ImportLabCatalogItemRequest
                {
                    Code = "DEMO-LAB-D-DIMER",
                    Name = "D-Dimer Kantitatif",
                    Category = "Koagülasyon",
                    SpecimenType = "Plazma",
                    ContainerType = "Mavi Kapaklı Sitratlı Tüp",
                    IsPanel = false,
                    TurnaroundMinutes = 30,
                    Description = "Tromboembolizm şüphesi için D-Dimer.",
                    Parameters =
                    [
                        new ImportLabCatalogParameterRequest
                        {
                            Code = "DDIM",
                            Name = "D-Dimer",
                            Unit = "µg/FEU/mL",
                            ReferenceRangeLow = 0m,
                            ReferenceRangeHigh = 0.5m,
                            CriticalHigh = 5.0m,
                            ValueType = "Numeric",
                            SortOrder = 1,
                        },
                    ],
                },
            ],
        };

        var importResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/diagnostics/lab-catalog/import", importReq);
        Assert.Equal(HttpStatusCode.OK, importResp.StatusCode);
        var importResult = await importResp.Content.ReadFromJsonAsync<ImportLabCatalogResponse>();
        Assert.NotNull(importResult);
        Assert.Equal("DEMO-LAB-2026.2-CUSTOM", importResult.CatalogVersion);
        Assert.Equal(1, importResult.TotalAdded);

        // 5. Verify import audit log
        await using var scope = application.Services.CreateAsyncScope();
        var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var logs = await auditDb.AuditLogs.AsNoTracking().ToListAsync();
        Assert.Contains(logs, l => l.Action == "Diagnostics.LabCatalogImport");
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
