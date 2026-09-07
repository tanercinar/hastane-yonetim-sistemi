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

public sealed class PharmacyWorklistIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G05")]
    public async Task PharmacistCanQueryWorklistAndFilterByStatusAndPrescriptionNumber()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        // 1. Doctor logs in and creates 2 prescriptions: 1 Draft, 1 Signed
        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var deptId = Guid.Parse("30000000-0000-0000-0000-000000000003");

        Guid amoxId;
        Guid parId;
        using (var scope = application.Services.CreateScope())
        {
            var pharmacyDb = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var amox = await pharmacyDb.MedicationCatalogItems.FirstAsync(m => m.Code == "DEMO-MED-AMX500");
            var par = await pharmacyDb.MedicationCatalogItems.FirstAsync(m => m.Code == "DEMO-MED-PAR500");
            amoxId = amox.Id;
            parId = par.Id;
        }

        // Draft Rx 1
        var encounterId1 = await ClinicalTestData.SeedStartedEncounterAsync(application);
        var encounterId2 = await ClinicalTestData.SeedStartedEncounterAsync(application);

        var draftReq1 = new CreatePrescriptionDraftRequest
        {
            EncounterId = encounterId1,
            PatientId = patientId,
            DepartmentId = deptId,
            DiagnosisSummary = "Akut Sinüzit",
            GeneralInstructions = "Günde 2 kez",
            Items =
            [
                new CreatePrescriptionDraftItemRequest
                {
                    MedicationCatalogItemId = amoxId,
                    Dose = 500,
                    DoseUnit = "mg",
                    Frequency = "2x1",
                    DurationDays = 7,
                    Quantity = 1,
                    QuantityUnit = "kutu",
                },
            ],
        };

        var draft1Res = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/pharmacy/prescriptions", draftReq1);
        Assert.Equal(HttpStatusCode.Created, draft1Res.StatusCode);
        var draft1 = await draft1Res.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(draft1);

        // Rx 2 (Draft -> Signed)
        var draftReq2 = new CreatePrescriptionDraftRequest
        {
            EncounterId = encounterId2,
            PatientId = patientId,
            DepartmentId = deptId,
            DiagnosisSummary = "Gerilim Tipi Baş Ağrısı",
            GeneralInstructions = "Ağrı halinde",
            Items =
            [
                new CreatePrescriptionDraftItemRequest
                {
                    MedicationCatalogItemId = parId,
                    Dose = 500,
                    DoseUnit = "mg",
                    Frequency = "1x1",
                    DurationDays = 5,
                    Quantity = 1,
                    QuantityUnit = "kutu",
                },
            ],
        };

        var draft2Res = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/pharmacy/prescriptions", draftReq2);
        Assert.Equal(HttpStatusCode.Created, draft2Res.StatusCode);
        var draft2 = await draft2Res.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(draft2);

        var signRes2 = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/pharmacy/prescriptions/{draft2.Id}/sign",
            new SignPrescriptionRequest
            {
                ExpectedVersion = draft2.Version,
                ValidDays = 14,
            });
        Assert.Equal(HttpStatusCode.OK, signRes2.StatusCode);

        // 2. Pharmacist logs in
        var pharmacistClient = CreateSecureClient(application);
        var pharmLogin = await LoginAsync(pharmacistClient, "DEMO-pharmacist@hospital.invalid", "DEMO-Pharm-Pass!1");
        Assert.Equal(HttpStatusCode.OK, pharmLogin.StatusCode);

        // 3. Query default worklist (active: Signed or PartiallyDispensed)
        var worklistResp = await pharmacistClient.GetAsync("/api/v1/pharmacy/prescriptions/worklist");
        Assert.Equal(HttpStatusCode.OK, worklistResp.StatusCode);
        var worklist = await worklistResp.Content.ReadFromJsonAsync<List<PrescriptionSummaryResponse>>();
        Assert.NotNull(worklist);

        // Should contain signed prescription 2, but NOT draft prescription 1
        Assert.Contains(worklist, rx => rx.Id == draft2.Id);
        Assert.DoesNotContain(worklist, rx => rx.Id == draft1.Id);

        // 4. Pharmacist inspects prescription detail
        var detailResp = await pharmacistClient.GetAsync($"/api/v1/pharmacy/prescriptions/{draft2.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResp.StatusCode);
        var detail = await detailResp.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(detail);
        Assert.Null(detail.DiagnosisSummary);
        Assert.Equal(Guid.Empty, detail.EncounterId);
        Assert.Single(detail.Items);
        Assert.Equal("DEMO-MED-PAR500", detail.Items[0].MedicationCode);

        // 5. Verify audit logs
        await using var scope2 = application.Services.CreateAsyncScope();
        var auditDb = scope2.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var auditLogs = await auditDb.AuditLogs.AsNoTracking().ToListAsync();

        Assert.Contains(auditLogs, a => a.Action == "Pharmacy.WorklistView");
        Assert.Contains(auditLogs, a => a.Action == "Pharmacy.PrescriptionView" && a.TargetResourceId == draft2.Id.ToString());
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
