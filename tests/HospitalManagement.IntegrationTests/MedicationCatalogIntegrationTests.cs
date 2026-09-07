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
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class MedicationCatalogIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G01")]
    public async Task SearchMedicationsReturnsFilteredResultsByQueryRouteAndForm()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        var loginResp = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        // 1. Search by Generic Name
        var amxResp = await doctorClient.GetAsync("/api/v1/pharmacy/medications?query=amoksisilin");
        Assert.Equal(HttpStatusCode.OK, amxResp.StatusCode);
        var amxItems = await amxResp.Content.ReadFromJsonAsync<List<MedicationCatalogItemResponse>>();
        Assert.NotNull(amxItems);
        Assert.Single(amxItems);
        Assert.Equal("DEMO-MED-AMX500", amxItems[0].Code);
        Assert.Equal("Amoksisilin", amxItems[0].GenericName);
        Assert.Equal("Oral", amxItems[0].Route);
        Assert.Equal("Capsule", amxItems[0].Form);

        // 2. Search by Route (Inhalation)
        var inhalerResp = await doctorClient.GetAsync("/api/v1/pharmacy/medications?route=Inhalation");
        Assert.Equal(HttpStatusCode.OK, inhalerResp.StatusCode);
        var inhalerItems = await inhalerResp.Content.ReadFromJsonAsync<List<MedicationCatalogItemResponse>>();
        Assert.NotNull(inhalerItems);
        Assert.Contains(inhalerItems, m => m.Code == "DEMO-MED-SAL100" && m.GenericName == "Salbutamol");

        // 3. Search by Form (Injection)
        var injectionResp = await doctorClient.GetAsync("/api/v1/pharmacy/medications?form=Injection");
        Assert.Equal(HttpStatusCode.OK, injectionResp.StatusCode);
        var injectionItems = await injectionResp.Content.ReadFromJsonAsync<List<MedicationCatalogItemResponse>>();
        Assert.NotNull(injectionItems);
        Assert.Contains(injectionItems, m => m.Code == "DEMO-MED-INS100" && m.GenericName == "İnsülin Glargin");

        // 4. Search by ATC code
        var atcResp = await doctorClient.GetAsync("/api/v1/pharmacy/medications?query=J01MA02");
        Assert.Equal(HttpStatusCode.OK, atcResp.StatusCode);
        var atcItems = await atcResp.Content.ReadFromJsonAsync<List<MedicationCatalogItemResponse>>();
        Assert.NotNull(atcItems);
        Assert.Single(atcItems);
        Assert.Equal("Siprofloksasin", atcItems[0].GenericName);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G01")]
    public async Task GetMedicationByIdReturnsItemOr404NotFound()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");

        var knownId = Guid.Parse("00000000-0000-0000-0000-000000000601");
        var successResp = await doctorClient.GetAsync($"/api/v1/pharmacy/medications/{knownId}");
        Assert.Equal(HttpStatusCode.OK, successResp.StatusCode);
        var item = await successResp.Content.ReadFromJsonAsync<MedicationCatalogItemResponse>();
        Assert.NotNull(item);
        Assert.Equal(knownId, item.Id);
        Assert.Equal("DEMO-MED-AMX500", item.Code);

        var unknownId = Guid.NewGuid();
        var notFoundResp = await doctorClient.GetAsync($"/api/v1/pharmacy/medications/{unknownId}");
        Assert.Equal(HttpStatusCode.NotFound, notFoundResp.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G01")]
    public async Task ImportMedicationCatalogSupportsVersioningAndAuditLogging()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var adminClient = CreateSecureClient(application);
        var loginResp = await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1");
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var newVersion = "DEMO-MED-2026.2";
        var importRequest = new ImportMedicationCatalogRequest
        {
            CatalogVersion = newVersion,
            Items =
            [
                new MedicationCatalogItemImportRequest
                {
                    Code = "DEMO-MED-NEW001",
                    BrandName = "DEMO-Levotiroksin 100mcg Tablet",
                    GenericName = "Levotiroksin Sodyum",
                    Form = "Tablet",
                    StrengthValue = 100m,
                    StrengthUnit = "mcg",
                    Route = "Oral",
                    AtcCode = "H03AA01",
                    Description = "Sentetik tiroid hormonu.",
                },
                new MedicationCatalogItemImportRequest
                {
                    Code = "DEMO-MED-NEW002",
                    BrandName = "DEMO-Furosemid 40mg Tablet",
                    GenericName = "Furosemid",
                    Form = "Tablet",
                    StrengthValue = 40m,
                    StrengthUnit = "mg",
                    Route = "Oral",
                    AtcCode = "C03CA01",
                    Description = "Kıvrım diüretiği antihipertansif.",
                },
            ],
        };

        var importResponse = await PostWithAntiforgeryAsync(
            adminClient,
            "/api/v1/pharmacy/medications/import",
            importRequest);

        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
        var importResult = await importResponse.Content.ReadFromJsonAsync<MedicationCatalogImportResultResponse>();
        Assert.NotNull(importResult);
        Assert.Equal(newVersion, importResult.CatalogVersion);
        Assert.Equal(2, importResult.TotalItems);
        Assert.Equal(2, importResult.InsertedCount);

        // Verify that doctor can now search for newly imported medication
        var doctorClient = CreateSecureClient(application);
        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");

        var searchResp = await doctorClient.GetAsync("/api/v1/pharmacy/medications?query=Levotiroksin");
        Assert.Equal(HttpStatusCode.OK, searchResp.StatusCode);
        var searchItems = await searchResp.Content.ReadFromJsonAsync<List<MedicationCatalogItemResponse>>();
        Assert.NotNull(searchItems);
        Assert.Single(searchItems);
        Assert.Equal("DEMO-MED-NEW001", searchItems[0].Code);
        Assert.Equal(newVersion, searchItems[0].CatalogVersion);

        // Verify Audit Log
        await using var verifyScope = application.Services.CreateAsyncScope();
        var auditDb = verifyScope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var auditLogs = await auditDb.AuditLogs.AsNoTracking().ToListAsync();
        Assert.Contains(auditLogs, a => a.Action == "Pharmacy.MedicationCatalogImport" && a.TargetResourceId == newVersion);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G01")]
    public async Task UnauthorizedUsersCannotImportMedicationCatalog()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        // 1. Unauthenticated client -> 401 Unauthorized
        var unauthClient = application.CreateClient();
        var unauthResp = await unauthClient.GetAsync("/api/v1/pharmacy/medications");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthResp.StatusCode);

        // 2. Patient client trying to import -> 403 Forbidden
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var importReq = new ImportMedicationCatalogRequest
        {
            CatalogVersion = "HACK-VER",
            Items =
            [
                new MedicationCatalogItemImportRequest
                {
                    Code = "HACK-01",
                    BrandName = "Fake Drug",
                    GenericName = "Fake Generic",
                    Form = "Tablet",
                    StrengthValue = 10,
                    StrengthUnit = "mg",
                    Route = "Oral",
                },
            ],
        };

        var patientImportResp = await PostWithAntiforgeryAsync(
            patientClient,
            "/api/v1/pharmacy/medications/import",
            importReq);

        Assert.Equal(HttpStatusCode.Forbidden, patientImportResp.StatusCode);
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

        var pharmacySeeder = scope.ServiceProvider.GetRequiredService<IMedicationCatalogDataSeeder>();
        await pharmacySeeder.SeedAsync();
    }
}
