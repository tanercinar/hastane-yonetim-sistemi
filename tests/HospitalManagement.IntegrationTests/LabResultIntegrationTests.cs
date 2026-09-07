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

public sealed class LabResultIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F06-G04")]
    public async Task LabResultCompleteLifecycleDraftTechnicalApproveClinicalApproveLockAndCorrectionWithAudit()
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
        var orderDraftReq = new CreateDiagnosticOrderDraftRequest
        {
            PatientId = Guid.Parse("00000000-0000-0000-0000-000000000109"),
            EncounterId = encounterId,
            DepartmentId = Guid.Parse("30000000-0000-0000-0000-000000000003"),
            OrderType = "Laboratory",
            Priority = "Routine",
            ClinicalIndication = "Diyabet ve anemi takibi",
            Items =
            [
                new() { CatalogCode = "DEMO-LAB-GLU", CatalogItemName = "Açlık Kan Şekeri (Glukoz)" },
            ],
        };

        var orderResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/diagnostics/orders", orderDraftReq);
        Assert.Equal(HttpStatusCode.Created, orderResp.StatusCode);
        var order = await orderResp.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>();
        Assert.NotNull(order);
        Assert.Single(order.Items);
        var orderItem = order.Items[0];

        var placeResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/diagnostics/orders/{order.Id}/place", new PlaceDiagnosticOrderRequest());
        Assert.Equal(HttpStatusCode.OK, placeResp.StatusCode);

        // 2. Lab Tech logs in and creates draft result
        var techClient = CreateSecureClient(application);
        await LoginAsync(techClient, "DEMO-labtech@hospital.invalid", "DEMO-LabTech-Pass!1");

        var draftResultReq = new CreateDraftLabResultRequest
        {
            DiagnosticOrderId = order.Id,
            DiagnosticOrderItemId = orderItem.Id,
            PatientId = Guid.Parse("00000000-0000-0000-0000-000000000109"),
            CatalogCode = "DEMO-LAB-GLU",
            CatalogItemName = "Açlık Kan Şekeri (Glukoz)",
            ClinicalNotes = "İlk çalışma",
            Items =
            [
                new() { ParameterCode = "GLU", NumericValue = 180m, Notes = "Tokluk şüphesi" },
            ],
        };

        var draftResp = await PostWithAntiforgeryAsync(techClient, "/api/v1/diagnostics/lab-results", draftResultReq);
        Assert.Equal(HttpStatusCode.Created, draftResp.StatusCode);
        var labResult = await draftResp.Content.ReadFromJsonAsync<LabResultDetailResponse>();
        Assert.NotNull(labResult);
        Assert.Equal("Draft", labResult.Status);
        Assert.Single(labResult.Items);
        Assert.Equal("High", labResult.Items[0].Flag);

        // The first result is the root of an immutable correction chain; a second root is rejected.
        var duplicateDraftResp = await PostWithAntiforgeryAsync(techClient, "/api/v1/diagnostics/lab-results", draftResultReq);
        Assert.Equal(HttpStatusCode.Conflict, duplicateDraftResp.StatusCode);

        // 3. Lab Tech updates values in draft
        var updateReq = new UpdateLabResultItemsRequest
        {
            ClinicalNotes = "Numune cihazda tekrar okutuldu",
            Items =
            [
                new() { ParameterCode = "GLU", NumericValue = 280m, Notes = "Kritik değer teyit edildi" },
            ],
        };

        var updateResp = await PutWithAntiforgeryAsync(techClient, $"/api/v1/diagnostics/lab-results/{labResult.Id}/items", updateReq);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updatedResult = await updateResp.Content.ReadFromJsonAsync<LabResultDetailResponse>();
        Assert.NotNull(updatedResult);
        Assert.Equal("CriticalHigh", updatedResult.Items[0].Flag);

        // 4. Lab Tech gives technical approval
        var techApproveResp = await PostWithAntiforgeryAsync<object?>(techClient, $"/api/v1/diagnostics/lab-results/{labResult.Id}/technical-approve", null);
        Assert.Equal(HttpStatusCode.OK, techApproveResp.StatusCode);
        var techApproved = await techApproveResp.Content.ReadFromJsonAsync<LabResultDetailResponse>();
        Assert.NotNull(techApproved);
        Assert.Equal("TechnicallyApproved", techApproved.Status);

        // 5. Doctor logs in and gives clinical approval (Finalize)
        var clinicalApproveResp = await PostWithAntiforgeryAsync<object?>(techClient, $"/api/v1/diagnostics/lab-results/{labResult.Id}/clinical-approve", null);
        Assert.Equal(HttpStatusCode.OK, clinicalApproveResp.StatusCode);
        var clinicallyApproved = await clinicalApproveResp.Content.ReadFromJsonAsync<LabResultDetailResponse>();
        Assert.NotNull(clinicallyApproved);
        Assert.Equal("FinalApproved", clinicallyApproved.Status);

        // 6. Direct edit on finalized result must fail (Immutability check)
        var directEditResp = await PutWithAntiforgeryAsync(techClient, $"/api/v1/diagnostics/lab-results/{labResult.Id}/items", updateReq);
        Assert.Equal(HttpStatusCode.BadRequest, directEditResp.StatusCode);

        // 7. Clinical correction
        var correctReq = new CorrectLabResultRequest
        {
            CorrectionReason = "Analizör kalibrasyon sapması nedeniyle yeniden çalışıldı.",
            CorrectedItems =
            [
                new() { ParameterCode = "GLU", NumericValue = 105m, Notes = "Düzeltilmiş sonuç" },
            ],
        };

        var correctResp = await PostWithAntiforgeryAsync(techClient, $"/api/v1/diagnostics/lab-results/{labResult.Id}/correct", correctReq);
        Assert.Equal(HttpStatusCode.Created, correctResp.StatusCode);
        var correctedResult = await correctResp.Content.ReadFromJsonAsync<LabResultDetailResponse>();
        Assert.NotNull(correctedResult);
        Assert.Equal("Corrected", correctedResult.Status);
        Assert.Equal(labResult.Id, correctedResult.PreviousResultId);
        Assert.Equal("Analizör kalibrasyon sapması nedeniyle yeniden çalışıldı.", correctedResult.CorrectionReason);
        Assert.Equal(105m, correctedResult.Items[0].NumericValue);
        Assert.Equal("High", correctedResult.Items[0].Flag);

        // 8. Verify audit logs
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var audits = await auditDb.AuditLogs
                .Where(a => a.TargetResourceType == "LabResult")
                .ToListAsync();

            Assert.Contains(audits, a => a.Action == "Diagnostics.LabResultCreateDraft");
            Assert.Contains(audits, a => a.Action == "Diagnostics.LabResultUpdateDraft");
            Assert.Contains(audits, a => a.Action == "Diagnostics.LabResultTechnicalApprove");
            Assert.Contains(audits, a => a.Action == "Diagnostics.LabResultClinicalApprove");
            Assert.Contains(audits, a => a.Action == "Diagnostics.LabResultCorrect");
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

    private static async Task<HttpResponseMessage> PutWithAntiforgeryAsync<TRequest>(
        HttpClient client,
        string requestUri,
        TRequest body)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/v1/identity/antiforgery");
        Assert.NotNull(token);
        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
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
