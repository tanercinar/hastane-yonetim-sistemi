using System.Net;
using System.Net.Http.Json;

using HospitalManagement.Contracts.Diagnostics;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
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

public sealed class DiagnosticOrderIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F06-G01")]
    public async Task DoctorCanCreatePlaceAndCancelDiagnosticOrderWithAudit()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(application, patientId);
        var deptId = Guid.Parse("30000000-0000-0000-0000-000000000003");

        // 1. Create draft order
        var createDraftReq = new CreateDiagnosticOrderDraftRequest
        {
            PatientId = patientId,
            EncounterId = encounterId,
            DepartmentId = deptId,
            OrderType = "Laboratory",
            Priority = "Routine",
            ClinicalIndication = "Anemi taraması",
            OrderNotes = "Sabah aç karnına kan alınacak",
            Items =
            [
                new CreateDiagnosticOrderItemRequest
                {
                    CatalogCode = "LAB-CBC",
                    CatalogItemName = "Tam Kan Sayımı (Hemogram)",
                    Category = "Hematology",
                    SpecialInstructions = "EDTA mor kapaklı tüp",
                },
                new CreateDiagnosticOrderItemRequest
                {
                    CatalogCode = "LAB-FE",
                    CatalogItemName = "Serum Demir & DDBK",
                    Category = "Biochemistry",
                    SpecialInstructions = "Sarı biyokimya tüpü",
                },
            ],
        };

        var createRes = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/diagnostics/orders", createDraftReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = await createRes.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>();
        Assert.NotNull(created);
        Assert.Equal("Draft", created.Status);
        Assert.Equal("Laboratory", created.OrderType);
        Assert.Equal(2, created.Items.Count);

        // 2. Place order
        var placeRes = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/diagnostics/orders/{created.Id}/place",
            new PlaceDiagnosticOrderRequest { Notes = "İstem onaylandı" });
        Assert.Equal(HttpStatusCode.OK, placeRes.StatusCode);
        var placed = await placeRes.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>();
        Assert.NotNull(placed);
        Assert.Equal("Placed", placed.Status);
        Assert.NotNull(placed.PlacedAtUtc);

        // 3. Query order by ID and Encounter
        var getByIdRes = await doctorClient.GetAsync($"/api/v1/diagnostics/orders/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getByIdRes.StatusCode);

        var getByEncRes = await doctorClient.GetAsync($"/api/v1/diagnostics/orders/by-encounter/{encounterId}");
        Assert.Equal(HttpStatusCode.OK, getByEncRes.StatusCode);
        var encOrders = await getByEncRes.Content.ReadFromJsonAsync<List<DiagnosticOrderSummaryResponse>>();
        Assert.NotNull(encOrders);
        Assert.Contains(encOrders, o => o.Id == created.Id);

        // 4. Cancel order
        var cancelRes = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/diagnostics/orders/{created.Id}/cancel",
            new CancelDiagnosticOrderRequest { Reason = "Klinik muayene revize edildi" });
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);
        var cancelled = await cancelRes.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>();
        Assert.NotNull(cancelled);
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal("Klinik muayene revize edildi", cancelled.CancellationReason);

        // 5. Verify Audit Logs
        await using var scope = application.Services.CreateAsyncScope();
        var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var logs = await auditDb.AuditLogs.AsNoTracking().ToListAsync();

        Assert.Contains(logs, l => l.Action == "Diagnostics.OrderCreateDraft" && l.TargetResourceId == created.Id.ToString());
        Assert.Contains(logs, l => l.Action == "Diagnostics.OrderPlace" && l.TargetResourceId == created.Id.ToString());
        Assert.Contains(logs, l => l.Action == "Diagnostics.OrderCancel" && l.TargetResourceId == created.Id.ToString());
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
    }
}
