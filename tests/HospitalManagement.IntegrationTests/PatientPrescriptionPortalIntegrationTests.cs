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

public sealed class PatientPrescriptionPortalIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G08")]
    public async Task PatientCanViewOwnSignedPrescriptionsAndDraftsAreHidden()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        // 1. Doctor creates 2 prescriptions for seeded demo patient: 1 Draft, 1 Signed
        var doctorClient = CreateSecureClient(application);
        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var deptId = Guid.Parse("30000000-0000-0000-0000-000000000003");

        Guid amoxId;
        using (var scope = application.Services.CreateScope())
        {
            var pharmacyDb = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var amox = await pharmacyDb.MedicationCatalogItems.FirstAsync(m => m.Code == "DEMO-MED-AMX500");
            amoxId = amox.Id;
        }

        // Draft Rx 1
        var encounterId1 = await ClinicalTestData.SeedStartedEncounterAsync(application);
        var encounterId2 = await ClinicalTestData.SeedStartedEncounterAsync(application);

        var draftReq = new CreatePrescriptionDraftRequest
        {
            EncounterId = encounterId1,
            PatientId = patientId,
            DepartmentId = deptId,
            DiagnosisSummary = "Taslak Tanı",
            GeneralInstructions = "Taslak Talimat",
            Items =
            [
                new CreatePrescriptionDraftItemRequest
                {
                    MedicationCatalogItemId = amoxId,
                    Dose = 500,
                    DoseUnit = "mg",
                    Frequency = "2x1",
                    DurationDays = 5,
                    Quantity = 1,
                    QuantityUnit = "kutu",
                },
            ],
        };
        var draftRes = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/pharmacy/prescriptions", draftReq);
        Assert.Equal(HttpStatusCode.Created, draftRes.StatusCode);
        var draft = await draftRes.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(draft);

        // Signed Rx 2
        var signedReq = new CreatePrescriptionDraftRequest
        {
            EncounterId = encounterId2,
            PatientId = patientId,
            DepartmentId = deptId,
            DiagnosisSummary = "İmzalı Tanı",
            GeneralInstructions = "İmzalı Talimat",
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
        var signedRes = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/pharmacy/prescriptions", signedReq);
        Assert.Equal(HttpStatusCode.Created, signedRes.StatusCode);
        var signedRx = await signedRes.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(signedRx);

        var signRes = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/pharmacy/prescriptions/{signedRx.Id}/sign",
            new SignPrescriptionRequest
            {
                ExpectedVersion = signedRx.Version,
                ValidDays = 14,
            });
        Assert.Equal(HttpStatusCode.OK, signRes.StatusCode);

        // 2. Patient logs in
        var patientClient = CreateSecureClient(application);
        var patLogin = await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");
        Assert.Equal(HttpStatusCode.OK, patLogin.StatusCode);

        // 3. Query prescriptions by patient ID
        var listResp = await patientClient.GetAsync($"/api/v1/pharmacy/prescriptions/by-patient/{patientId}");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var prescriptions = await listResp.Content.ReadFromJsonAsync<List<PrescriptionSummaryResponse>>();
        Assert.NotNull(prescriptions);

        // Signed prescription is visible, Draft prescription is hidden
        Assert.Contains(prescriptions, p => p.Id == signedRx.Id);
        Assert.DoesNotContain(prescriptions, p => p.Id == draft.Id);

        // 4. Patient opens detail of signed prescription
        var detailResp = await patientClient.GetAsync($"/api/v1/pharmacy/prescriptions/{signedRx.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResp.StatusCode);
        var detail = await detailResp.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(detail);
        Assert.Equal("İmzalı Tanı", detail.DiagnosisSummary);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G08")]
    public async Task PatientCannotAccessOtherPatientPrescriptionsIdorProtection()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        // Patient logs in (Patient ID: 00000000-0000-0000-0000-000000000109)
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        // Attempting to query another patient's ID
        var otherPatientId = Guid.NewGuid();
        var idorResp = await patientClient.GetAsync($"/api/v1/pharmacy/prescriptions/by-patient/{otherPatientId}");
        Assert.Equal(HttpStatusCode.Forbidden, idorResp.StatusCode);
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
