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

public sealed class DicomSimulationIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F06-G07")]
    public async Task DicomMetadataAndSignedPreviewAccessWithIdorAndAudit()
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
            OrderType = "Radiology",
            Priority = "Routine",
            ClinicalIndication = "Kranial MR İnceleme",
            Items =
            [
                new() { CatalogCode = "DEMO-RAD-BRAIN-MRI", CatalogItemName = "Beyin Manyetik Rezonans Görüntüleme (Kranial MRG)" },
            ],
        };

        var orderResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/diagnostics/orders", orderDraftReq);
        Assert.Equal(HttpStatusCode.Created, orderResp.StatusCode);
        var order = await orderResp.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>();
        Assert.NotNull(order);

        await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/diagnostics/orders/{order.Id}/place", new PlaceDiagnosticOrderRequest());

        // 2. Radiology Staff ensures study, acquires and finalizes report
        var radClient = CreateSecureClient(application);
        await LoginAsync(radClient, "DEMO-radtech@hospital.invalid", "DEMO-RadTech-Pass!1");

        var ensureResp = await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/ensure?orderId={order.Id}&orderItemId={order.Items[0].Id}", new
        {
        });
        var study = await ensureResp.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>();
        Assert.NotNull(study);

        var scheduleResp = await PostWithAntiforgeryAsync(
            radClient,
            $"/api/v1/diagnostics/radiology/studies/{study.Id}/schedule",
            new ScheduleRadiologyStudyRequest { ScheduledAtUtc = DateTime.UtcNow.AddMinutes(5) });
        Assert.Equal(HttpStatusCode.OK, scheduleResp.StatusCode);

        await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/{study.Id}/complete-acquisition", new CompleteAcquisitionRequest { TechnicianNotes = "T1/T2/FLAIR kesitler alındı." });
        await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/{study.Id}/finalize-report", new FinalizeRadiologyReportRequest { ReportText = "Normal kranial MRG.", Impression = "Patoloji saptanmadı." });

        // 3. Radiology staff queries DICOM metadata
        var metaResp = await radClient.GetAsync($"/api/v1/diagnostics/radiology/studies/{study.Id}/dicom-metadata");
        Assert.Equal(HttpStatusCode.OK, metaResp.StatusCode);
        var metadata = await metaResp.Content.ReadFromJsonAsync<DicomStudyMetadataResponse>();
        Assert.NotNull(metadata);
        Assert.True(metadata.IsMockSimulation);
        Assert.NotEmpty(metadata.Series);
        var firstInstance = metadata.Series[0].Instances[0];
        Assert.NotEmpty(firstInstance.ViewToken);

        // 4. Request DICOM preview through an authenticated POST body; the token never enters a URL.
        var previewResp = await PostWithAntiforgeryAsync(
            radClient,
            "/api/v1/diagnostics/radiology/dicom-preview",
            new DicomPreviewImageRequest { Token = firstInstance.ViewToken });
        Assert.Equal(HttpStatusCode.OK, previewResp.StatusCode);
        Assert.Equal("image/svg+xml", previewResp.Content.Headers.ContentType?.MediaType);
        var svgStr = await previewResp.Content.ReadAsStringAsync();
        Assert.Contains("MOCK DICOM PREVIEW", svgStr, StringComparison.Ordinal);
        Assert.Contains("MR", svgStr, StringComparison.Ordinal);

        // 5. Tampered token is rejected
        var tamperedResp = await PostWithAntiforgeryAsync(
            radClient,
            "/api/v1/diagnostics/radiology/dicom-preview",
            new DicomPreviewImageRequest { Token = "invalid.tampered" });
        Assert.Equal(HttpStatusCode.BadRequest, tamperedResp.StatusCode);

        // 6. Patient can view own metadata
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var patientMetaResp = await patientClient.GetAsync($"/api/v1/diagnostics/radiology/studies/{study.Id}/dicom-metadata");
        Assert.Equal(HttpStatusCode.OK, patientMetaResp.StatusCode);

        var transferredTokenResp = await PostWithAntiforgeryAsync(
            patientClient,
            "/api/v1/diagnostics/radiology/dicom-preview",
            new DicomPreviewImageRequest { Token = firstInstance.ViewToken });
        Assert.Equal(HttpStatusCode.Forbidden, transferredTokenResp.StatusCode);

        // 7. Audit log verification
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var audits = await auditDb.AuditLogs
                .Where(a => a.TargetResourceType == "DicomStudy")
                .ToListAsync();

            Assert.Contains(audits, a => a.Action == "Diagnostics.DicomMetadataAccess");
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
    }
}
