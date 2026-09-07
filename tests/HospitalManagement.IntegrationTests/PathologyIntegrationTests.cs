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

public sealed class PathologyIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F06-G08")]
    public async Task CompletePathologyWorkflowFromOrderToReportCorrectionAndAudit()
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

        // 1. Doctor creates and places Pathology order
        var orderDraftReq = new CreateDiagnosticOrderDraftRequest
        {
            PatientId = Guid.Parse("00000000-0000-0000-0000-000000000109"),
            EncounterId = encounterId,
            DepartmentId = Guid.Parse("30000000-0000-0000-0000-000000000003"),
            OrderType = "Pathology",
            Priority = "Routine",
            ClinicalIndication = "Mide endoskopik biyopsi incelemesi",
            Items =
            [
                new() { CatalogCode = "DEMO-PAT-BIOPSY-GASTRIC", CatalogItemName = "Endoskopik Mide Mukozası Biyopsisi" },
            ],
        };

        var orderResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/diagnostics/orders", orderDraftReq);
        Assert.Equal(HttpStatusCode.Created, orderResp.StatusCode);
        var order = await orderResp.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>();
        Assert.NotNull(order);

        await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/diagnostics/orders/{order.Id}/place", new PlaceDiagnosticOrderRequest());

        // 2. Ensure Pathology case
        var labClient = CreateSecureClient(application);
        await LoginAsync(labClient, "DEMO-labtech@hospital.invalid", "DEMO-LabTech-Pass!1");

        var ensureResp = await PostWithAntiforgeryAsync(labClient, $"/api/v1/diagnostics/pathology/cases/ensure?orderId={order.Id}&orderItemId={order.Items[0].Id}&specimenType=Biopsy&anatomicSite=Mide%20Antrum", new
        {
        });
        Assert.Equal(HttpStatusCode.OK, ensureResp.StatusCode);
        var pathCase = await ensureResp.Content.ReadFromJsonAsync<PathologyCaseDetailResponse>();
        Assert.NotNull(pathCase);
        Assert.Equal("Ordered", pathCase.Status);

        // 3. Receive Specimen
        var receiveResp = await PostWithAntiforgeryAsync(labClient, $"/api/v1/diagnostics/pathology/cases/{pathCase.Id}/receive-specimen", new ReceivePathologySpecimenRequest { FixativeUsed = "%10 Nötral Tamponlu Formalin" });
        Assert.Equal(HttpStatusCode.OK, receiveResp.StatusCode);
        var receivedCase = await receiveResp.Content.ReadFromJsonAsync<PathologyCaseDetailResponse>();
        Assert.NotNull(receivedCase);
        Assert.Equal("SpecimenReceived", receivedCase.Status);

        // 4. Record Gross Exam
        var grossResp = await PostWithAntiforgeryAsync(labClient, $"/api/v1/diagnostics/pathology/cases/{pathCase.Id}/gross-exam", new RecordGrossExamRequest { GrossDescription = "2 adet kirli beyaz renkli doku parçası." });
        Assert.Equal(HttpStatusCode.OK, grossResp.StatusCode);

        // 5. Record Microscopic Exam
        var microResp = await PostWithAntiforgeryAsync(labClient, $"/api/v1/diagnostics/pathology/cases/{pathCase.Id}/microscopic-exam", new RecordMicroscopicExamRequest { MicroscopicDescription = "Lamina propriada belirgin mononükleer inflamatuar hücre infiltrasyonu." });
        Assert.Equal(HttpStatusCode.OK, microResp.StatusCode);

        // 6. Pathologist drafts and finalizes report
        var draftResp = await PostWithAntiforgeryAsync(labClient, $"/api/v1/diagnostics/pathology/cases/{pathCase.Id}/draft-report", new DraftPathologyReportRequest { PathologicalDiagnosis = "KRONİK NON-SPESİFİK GASTRİT TASLAK" });
        Assert.Equal(HttpStatusCode.OK, draftResp.StatusCode);

        var finalizeResp = await PostWithAntiforgeryAsync(labClient, $"/api/v1/diagnostics/pathology/cases/{pathCase.Id}/finalize-report", new FinalizePathologyReportRequest { PathologicalDiagnosis = "KRONİK AKTİF GASTRİT (H. Pylori negatif)" });
        Assert.Equal(HttpStatusCode.OK, finalizeResp.StatusCode);
        var finalizedCase = await finalizeResp.Content.ReadFromJsonAsync<PathologyCaseDetailResponse>();
        Assert.NotNull(finalizedCase);
        Assert.Equal("ReportFinalized", finalizedCase.Status);

        // 7. Correct Report with mandatory justification
        var correctResp = await PostWithAntiforgeryAsync(labClient, $"/api/v1/diagnostics/pathology/cases/{pathCase.Id}/correct-report", new CorrectPathologyReportRequest
        {
            CorrectionReason = "Giemsa histokimyasal boyama ile mikroorganizma varlığı teyit edildi.",
            NewDiagnosis = "KRONİK AKTİF GASTRİT (H. Pylori POZİTİF)",
        });
        Assert.Equal(HttpStatusCode.OK, correctResp.StatusCode);
        var correctedCase = await correctResp.Content.ReadFromJsonAsync<PathologyCaseDetailResponse>();
        Assert.NotNull(correctedCase);
        Assert.Equal("Corrected", correctedCase.Status);
        Assert.Equal(pathCase.Id, correctedCase.PreviousCaseId);

        // 8. Patient can view finalized/corrected report
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var patientCaseResp = await patientClient.GetAsync($"/api/v1/diagnostics/pathology/cases/{correctedCase.Id}");
        Assert.Equal(HttpStatusCode.OK, patientCaseResp.StatusCode);

        // 9. Verify Audit Logs
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var audits = await auditDb.AuditLogs
                .Where(a => a.TargetResourceType == "PathologyCase")
                .ToListAsync();

            Assert.Contains(audits, a => a.Action == "Diagnostics.PathologyCaseCreate");
            Assert.Contains(audits, a => a.Action == "Diagnostics.PathologySpecimenReceive");
            Assert.Contains(audits, a => a.Action == "Diagnostics.PathologyGrossExamRecord");
            Assert.Contains(audits, a => a.Action == "Diagnostics.PathologyMicroscopicExamRecord");
            Assert.Contains(audits, a => a.Action == "Diagnostics.PathologyReportFinalize");
            Assert.Contains(audits, a => a.Action == "Diagnostics.PathologyReportCorrect");
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
