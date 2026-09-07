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

public sealed class DiagnosticTimelineIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F06-G10")]
    public async Task DiagnosticTimelineAndPatientPortalEnsureDraftHidingAndApprovedVisibility()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);
        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");

        var doctorClient = CreateSecureClient(application);
        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");

        // 1. Doctor creates and places separate Lab and Radiology orders.
        var orderDraftReq = new CreateDiagnosticOrderDraftRequest
        {
            PatientId = patientId,
            EncounterId = encounterId,
            DepartmentId = Guid.Parse("30000000-0000-0000-0000-000000000003"),
            OrderType = "Laboratory",
            Priority = "Routine",
            ClinicalIndication = "Rutin kontrol ve PA Akciğer Grafisi",
            Items =
            [
                new() { CatalogCode = "DEMO-LAB-CBC", CatalogItemName = "Tam Kan Sayımı (Hemogram)" },
            ],
        };

        var orderResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/diagnostics/orders", orderDraftReq);
        Assert.Equal(HttpStatusCode.Created, orderResp.StatusCode);
        var order = await orderResp.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>();
        Assert.NotNull(order);

        await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/diagnostics/orders/{order.Id}/place", new PlaceDiagnosticOrderRequest());

        var radiologyOrderResp = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/diagnostics/orders",
            new CreateDiagnosticOrderDraftRequest
            {
                PatientId = patientId,
                EncounterId = encounterId,
                DepartmentId = Guid.Parse("30000000-0000-0000-0000-000000000003"),
                OrderType = "Radiology",
                Priority = "Routine",
                ClinicalIndication = "PA Akciğer Grafisi",
                Items =
                [
                    new() { CatalogCode = "DEMO-RAD-CHEST-XR", CatalogItemName = "Akciğer Grafisi (PA)" },
                ],
            });
        Assert.Equal(HttpStatusCode.Created, radiologyOrderResp.StatusCode);
        var radiologyOrder = await radiologyOrderResp.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>();
        Assert.NotNull(radiologyOrder);
        await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/diagnostics/orders/{radiologyOrder.Id}/place",
            new PlaceDiagnosticOrderRequest());

        // 2. Create Draft Lab Result & Schedule Radiology study (DRAFTS / IN-PROGRESS)
        var labClient = CreateSecureClient(application);
        await LoginAsync(labClient, "DEMO-labtech@hospital.invalid", "DEMO-LabTech-Pass!1");

        var draftLabReq = new CreateDraftLabResultRequest
        {
            DiagnosticOrderId = order.Id,
            DiagnosticOrderItemId = order.Items[0].Id,
            PatientId = patientId,
            CatalogCode = "DEMO-LAB-CBC",
            CatalogItemName = "Tam Kan Sayımı (Hemogram)",
            ClinicalNotes = "Numune işleniyor...",
            Items =
            [
                new() { ParameterCode = "WBC", NumericValue = 7.5m },
            ],
        };
        var draftLabResp = await PostWithAntiforgeryAsync(labClient, "/api/v1/diagnostics/lab-results", draftLabReq);
        Assert.Equal(HttpStatusCode.Created, draftLabResp.StatusCode);
        var labDraft = await draftLabResp.Content.ReadFromJsonAsync<LabResultDetailResponse>();
        Assert.NotNull(labDraft);

        var radClient = CreateSecureClient(application);
        await LoginAsync(radClient, "DEMO-radtech@hospital.invalid", "DEMO-RadTech-Pass!1");
        var radEnsureResp = await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/ensure?orderId={radiologyOrder.Id}&orderItemId={radiologyOrder.Items[0].Id}", new
        {
        });
        Assert.Equal(HttpStatusCode.OK, radEnsureResp.StatusCode);
        var radStudy = await radEnsureResp.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>();
        Assert.NotNull(radStudy);

        // 3. Patient logs in: Check that NO draft leaks to patient
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var patientPreResultsResp = await patientClient.GetAsync("/api/v1/diagnostics/results/my");
        Assert.Equal(HttpStatusCode.OK, patientPreResultsResp.StatusCode);
        var patientPreResults = await patientPreResultsResp.Content.ReadFromJsonAsync<List<PatientPortalResultSummaryResponse>>();
        Assert.NotNull(patientPreResults);
        Assert.Empty(patientPreResults); // Strictly 0 results while in draft/in-progress!

        // 4. Authorized LAB staff technically and clinically approves Lab Result.
        await PostWithAntiforgeryAsync(labClient, $"/api/v1/diagnostics/lab-results/{labDraft.Id}/technical-approve", new
        {
        });
        await PostWithAntiforgeryAsync(labClient, $"/api/v1/diagnostics/lab-results/{labDraft.Id}/clinical-approve", new
        {
        });

        // 5. Authorized RAD staff schedules, acquires and finalizes the Radiology report.
        await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/{radStudy.Id}/schedule", new ScheduleRadiologyStudyRequest
        {
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(5),
        });
        await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/{radStudy.Id}/complete-acquisition", new CompleteAcquisitionRequest { TechnicianNotes = "PA çekim yapıldı" });
        await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/{radStudy.Id}/finalize-report", new FinalizeRadiologyReportRequest
        {
            ReportText = "Kardiyotorasik indeks normal. Aktif infiltrasyon saptanmadı.",
            Impression = "Doğal Akciğer Grafisi",
        });

        // 6. Patient logs in again: Now sees both approved results!
        var patientPostResultsResp = await patientClient.GetAsync("/api/v1/diagnostics/results/my");
        Assert.Equal(HttpStatusCode.OK, patientPostResultsResp.StatusCode);
        var patientPostResults = await patientPostResultsResp.Content.ReadFromJsonAsync<List<PatientPortalResultSummaryResponse>>();
        Assert.NotNull(patientPostResults);
        Assert.Equal(2, patientPostResults.Count);
        Assert.Contains(patientPostResults, r => r.Category == "Laboratory");
        Assert.Contains(patientPostResults, r => r.Category == "Radiology");

        // 7. Doctor queries Patient Diagnostic Timeline
        var doctorTimelineResp = await doctorClient.GetAsync($"/api/v1/diagnostics/timeline/patient/{patientId}");
        Assert.Equal(HttpStatusCode.OK, doctorTimelineResp.StatusCode);
        var timeline = await doctorTimelineResp.Content.ReadFromJsonAsync<PatientDiagnosticTimelineResponse>();
        Assert.NotNull(timeline);
        Assert.Equal(2, timeline.TotalEntries);
        Assert.NotEmpty(timeline.Entries);

        // 8. Verify Audit Logs
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var audits = await auditDb.AuditLogs
                .Where(a => a.TargetResourceType == "PatientDiagnosticTimeline" || a.TargetResourceType == "PatientPortalResults")
                .ToListAsync();

            Assert.Contains(audits, a => a.Action == "Diagnostics.TimelineDoctorView");
            Assert.Contains(audits, a => a.Action == "Diagnostics.TimelinePatientPortalView");
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
