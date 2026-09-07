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

public sealed class CriticalResultIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F06-G05")]
    public async Task CriticalResultNotificationLifecycleCreationEscalationAndAcknowledgmentWithAudit()
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
            Priority = "Stat",
            ClinicalIndication = "Diyabetik koma şüphesi",
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

        // 2. Lab Tech enters critical high value (520 mg/dL)
        var techClient = CreateSecureClient(application);
        await LoginAsync(techClient, "DEMO-labtech@hospital.invalid", "DEMO-LabTech-Pass!1");

        var draftResultReq = new CreateDraftLabResultRequest
        {
            DiagnosticOrderId = order.Id,
            DiagnosticOrderItemId = orderItem.Id,
            PatientId = Guid.Parse("00000000-0000-0000-0000-000000000109"),
            CatalogCode = "DEMO-LAB-GLU",
            CatalogItemName = "Açlık Kan Şekeri (Glukoz)",
            ClinicalNotes = "Kritik panik değer çalışıldı",
            Items =
            [
                new() { ParameterCode = "GLU", NumericValue = 520m, Notes = "Tekrarlandı, doğrulandı" },
            ],
        };

        var draftResp = await PostWithAntiforgeryAsync(techClient, "/api/v1/diagnostics/lab-results", draftResultReq);
        Assert.Equal(HttpStatusCode.Created, draftResp.StatusCode);
        var labResult = await draftResp.Content.ReadFromJsonAsync<LabResultDetailResponse>();
        Assert.NotNull(labResult);
        Assert.Equal("CriticalHigh", labResult.Items[0].Flag);

        // 3. Doctor checks active critical notifications
        var activeAlertsResp = await doctorClient.GetAsync("/api/v1/diagnostics/critical-notifications/active");
        Assert.Equal(HttpStatusCode.OK, activeAlertsResp.StatusCode);
        var activeAlerts = await activeAlertsResp.Content.ReadFromJsonAsync<List<CriticalResultNotificationResponse>>();
        Assert.NotNull(activeAlerts);
        Assert.NotEmpty(activeAlerts);

        var alert = activeAlerts.First(a => a.LabResultId == labResult.Id);
        Assert.Equal("Active", alert.Status);
        Assert.Equal(1, alert.EscalationLevel);
        Assert.Equal("GLU", alert.ParameterCode);
        Assert.Equal(520m, alert.NumericValue);

        // 4. Escalate simulation
        var escalateReq = new EscalateCriticalResultRequest
        {
            EscalationReason = "Birincil hekim ameliyatta olduğu için nöbetçi uzmana yönlendirildi.",
        };

        var escalateResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/diagnostics/critical-notifications/{alert.Id}/escalate", escalateReq);
        Assert.Equal(HttpStatusCode.OK, escalateResp.StatusCode);
        var escalatedAlert = await escalateResp.Content.ReadFromJsonAsync<CriticalResultNotificationResponse>();
        Assert.NotNull(escalatedAlert);
        Assert.Equal("Escalated", escalatedAlert.Status);
        Assert.Equal(2, escalatedAlert.EscalationLevel);
        Assert.Equal("Birincil hekim ameliyatta olduğu için nöbetçi uzmana yönlendirildi.", escalatedAlert.EscalationReason);

        // 5. Doctor acknowledges critical finding
        var ackReq = new AcknowledgeCriticalResultRequest
        {
            AcknowledgmentNotes = "Sonuç nöbetçi hekim tarafından telefonla teslim alındı; hastaya acil insülin infüzyonu başlandı.",
        };

        var ackResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/diagnostics/critical-notifications/{alert.Id}/acknowledge", ackReq);
        Assert.Equal(HttpStatusCode.OK, ackResp.StatusCode);
        var acknowledgedAlert = await ackResp.Content.ReadFromJsonAsync<CriticalResultNotificationResponse>();
        Assert.NotNull(acknowledgedAlert);
        Assert.Equal("Acknowledged", acknowledgedAlert.Status);
        Assert.NotNull(acknowledgedAlert.AcknowledgedAtUtc);
        Assert.NotNull(acknowledgedAlert.AcknowledgedByUserId);
        Assert.Equal("Sonuç nöbetçi hekim tarafından telefonla teslim alındı; hastaya acil insülin infüzyonu başlandı.", acknowledgedAlert.AcknowledgmentNotes);

        // 6. Patient attempting acknowledgment must receive 403 Forbidden
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var patientAckResp = await PostWithAntiforgeryAsync(patientClient, $"/api/v1/diagnostics/critical-notifications/{alert.Id}/acknowledge", ackReq);
        Assert.Equal(HttpStatusCode.Forbidden, patientAckResp.StatusCode);

        // 7. Verify audit logs
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var audits = await auditDb.AuditLogs
                .Where(a => a.TargetResourceType == "CriticalResultNotification")
                .ToListAsync();

            Assert.Contains(audits, a => a.Action == "Diagnostics.CriticalResultNotify");
            Assert.Contains(audits, a => a.Action == "Diagnostics.CriticalResultEscalate");
            Assert.Contains(audits, a => a.Action == "Diagnostics.CriticalResultAcknowledge");
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
    }
}
