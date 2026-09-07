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

public sealed class RadiologyIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F06-G06")]
    public async Task CompleteRadiologyLifecycleFromOrderToReportAndAddendumWithAudit()
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

        // 1. Doctor creates and places Radiology Order
        var orderDraftReq = new CreateDiagnosticOrderDraftRequest
        {
            PatientId = Guid.Parse("00000000-0000-0000-0000-000000000109"),
            EncounterId = encounterId,
            DepartmentId = Guid.Parse("30000000-0000-0000-0000-000000000003"),
            OrderType = "Radiology",
            Priority = "Routine",
            ClinicalIndication = "Öksürük ve nefes darlığı değerlendirmesi",
            Items =
            [
                new() { CatalogCode = "DEMO-RAD-CHEST-XR", CatalogItemName = "Akciğer Grafisi (PA / Göğüs Radyografisi)" },
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

        // 2. Radiology Staff ensures study
        var radClient = CreateSecureClient(application);
        await LoginAsync(radClient, "DEMO-radtech@hospital.invalid", "DEMO-RadTech-Pass!1");

        var ensureResp = await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/ensure?orderId={order.Id}&orderItemId={orderItem.Id}", new
        {
        });
        Assert.Equal(HttpStatusCode.OK, ensureResp.StatusCode);
        var study = await ensureResp.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>();
        Assert.NotNull(study);
        Assert.StartsWith("DEMO-ACC-", study.AccessionNumber, StringComparison.Ordinal);
        Assert.Equal("Ordered", study.Status);
        Assert.Equal("XR", study.Modality);

        // Retrying intake is idempotent and returns the same study rather than duplicating it.
        var repeatedEnsureResp = await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/ensure?orderId={order.Id}&orderItemId={orderItem.Id}", new
        {
        });
        Assert.Equal(HttpStatusCode.OK, repeatedEnsureResp.StatusCode);
        var repeatedStudy = await repeatedEnsureResp.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>();
        Assert.NotNull(repeatedStudy);
        Assert.Equal(study.Id, repeatedStudy.Id);

        // 3. Technician schedules study
        var scheduleReq = new ScheduleRadiologyStudyRequest
        {
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
        };
        var scheduleResp = await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/{study.Id}/schedule", scheduleReq);
        Assert.Equal(HttpStatusCode.OK, scheduleResp.StatusCode);
        var scheduledStudy = await scheduleResp.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>();
        Assert.NotNull(scheduledStudy);
        Assert.Equal("Scheduled", scheduledStudy.Status);

        // 4. Technician completes image acquisition
        var acqReq = new CompleteAcquisitionRequest
        {
            TechnicianNotes = "PA grafi çekildi, hareket artefaktı izlenmedi.",
        };
        var acqResp = await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/{study.Id}/complete-acquisition", acqReq);
        Assert.Equal(HttpStatusCode.OK, acqResp.StatusCode);
        var acqStudy = await acqResp.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>();
        Assert.NotNull(acqStudy);
        Assert.Equal("Acquired", acqStudy.Status);

        // 5. Radiologist drafts report
        var draftReportReq = new DraftRadiologyReportRequest
        {
            ReportText = "Bilateral akciğer parankim alanlarında aktif infiltratif lezyon izlenmemiştir.",
            Impression = "Doğal sınırlarda PA Akciğer Grafisi",
        };
        var draftResp = await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/{study.Id}/draft-report", draftReportReq);
        Assert.Equal(HttpStatusCode.OK, draftResp.StatusCode);
        var draftedStudy = await draftResp.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>();
        Assert.NotNull(draftedStudy);
        Assert.Equal("ReportDrafted", draftedStudy.Status);

        // 6. Patient cannot view draft report
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var patientDraftViewResp = await patientClient.GetAsync($"/api/v1/diagnostics/radiology/studies/{study.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, patientDraftViewResp.StatusCode);

        // 7. Radiologist finalizes report
        var finalReq = new FinalizeRadiologyReportRequest
        {
            ReportText = "Bilateral akciğer parankim alanlarında aktif konsolidasyon veya infiltrasyon saptanmamıştır. Kardiyotorasik indeks normal sınırlardadır. Kostodiafragmatik sinüsler açıktır.",
            Impression = "Normal sınırlarda PA Akciğer Grafisi.",
        };
        var finalResp = await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/{study.Id}/finalize-report", finalReq);
        Assert.Equal(HttpStatusCode.OK, finalResp.StatusCode);
        var finalStudy = await finalResp.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>();
        Assert.NotNull(finalStudy);
        Assert.Equal("ReportFinalized", finalStudy.Status);
        Assert.NotNull(finalStudy.ReportFinalizedAtUtc);

        // 8. Patient can now view finalized report
        var patientFinalViewResp = await patientClient.GetAsync($"/api/v1/diagnostics/radiology/studies/{study.Id}");
        Assert.Equal(HttpStatusCode.OK, patientFinalViewResp.StatusCode);
        var patientViewStudy = await patientFinalViewResp.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>();
        Assert.NotNull(patientViewStudy);
        Assert.Equal("ReportFinalized", patientViewStudy.Status);

        // 9. Radiologist adds signed addendum
        var addendumReq = new AddRadiologyAddendumRequest
        {
            AddendumText = "Klinik hekimin ek sorusu üzerine apekste plevral kalınlaşma olmadığı teyit edilmiştir.",
        };
        var addendumResp = await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/{study.Id}/add-addendum", addendumReq);
        Assert.Equal(HttpStatusCode.OK, addendumResp.StatusCode);
        var addendumStudy = await addendumResp.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>();
        Assert.NotNull(addendumStudy);
        Assert.Equal("AddendumAdded", addendumStudy.Status);
        Assert.Contains("plevral kalınlaşma olmadığı teyit edilmiştir.", addendumStudy.AddendumText, StringComparison.Ordinal);

        // 10. Direct overwrite or cancellation after final is rejected
        var cancelResp = await PostWithAntiforgeryAsync(radClient, $"/api/v1/diagnostics/radiology/studies/{study.Id}/cancel", new CancelRadiologyStudyRequest { Reason = "İptal denemesi" });
        Assert.Equal(HttpStatusCode.BadRequest, cancelResp.StatusCode);

        // 11. Verify Audit Logs
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var audits = await auditDb.AuditLogs
                .Where(a => a.TargetResourceType == "RadiologyStudy")
                .ToListAsync();

            Assert.Contains(audits, a => a.Action == "Diagnostics.RadiologyStudyCreate");
            Assert.Contains(audits, a => a.Action == "Diagnostics.RadiologyStudySchedule");
            Assert.Contains(audits, a => a.Action == "Diagnostics.RadiologyAcquisitionComplete");
            Assert.Contains(audits, a => a.Action == "Diagnostics.RadiologyReportDraft");
            Assert.Contains(audits, a => a.Action == "Diagnostics.RadiologyReportFinalize");
            Assert.Contains(audits, a => a.Action == "Diagnostics.RadiologyReportAddendum");
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
