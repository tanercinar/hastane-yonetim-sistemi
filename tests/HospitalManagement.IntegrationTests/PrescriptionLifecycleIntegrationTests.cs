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

public sealed class PrescriptionLifecycleIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G02")]
    public async Task PrescriptionFullLifecycleDraftSignDispenseCancelAndAudit()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var patientPersonId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(application);
        var departmentId = Guid.Parse("30000000-0000-0000-0000-000000000003"); // Cardiology
        var medCatalogItemId = Guid.Parse("00000000-0000-0000-0000-000000000601"); // Amoxicillin

        // 1. Create Prescription Draft
        var draftReq = new CreatePrescriptionDraftRequest
        {
            PatientId = patientPersonId,
            EncounterId = encounterId,
            DepartmentId = departmentId,
            DiagnosisSummary = "Akut Üst Solunum Yolu Enfeksiyonu",
            GeneralInstructions = "Günde 2 kez tok karnına 1 kapsül alınız.",
            Items =
            [
                new CreatePrescriptionDraftItemRequest
                {
                    MedicationCatalogItemId = medCatalogItemId,
                    Dose = 500m,
                    DoseUnit = "mg",
                    Frequency = "2x1",
                    DurationDays = 7,
                    Quantity = 1,
                    QuantityUnit = "kutu",
                    Instructions = "Yemekten sonra bol su ile.",
                },
            ],
        };

        var draftResp = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/pharmacy/prescriptions",
            draftReq);

        Assert.Equal(HttpStatusCode.Created, draftResp.StatusCode);
        var createdRx = await draftResp.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(createdRx);
        Assert.Equal("Draft", createdRx.Status);
        Assert.Equal(1, createdRx.Version);
        Assert.Single(createdRx.Items);
        Assert.Equal("DEMO-MED-AMX500", createdRx.Items[0].MedicationCode);
        Assert.Equal("Amoksisilin", createdRx.Items[0].GenericName);

        var rxId = createdRx.Id;

        // 2. Doctor signs the prescription
        var signReq = new SignPrescriptionRequest
        {
            ExpectedVersion = createdRx.Version,
            ValidDays = 10,
        };
        var signResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/pharmacy/prescriptions/{rxId}/sign",
            signReq);

        Assert.Equal(HttpStatusCode.OK, signResp.StatusCode);
        var signedRx = await signResp.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(signedRx);
        Assert.Equal("Signed", signedRx.Status);
        Assert.Equal(2, signedRx.Version);
        Assert.NotNull(signedRx.SignedAtUtc);
        Assert.NotNull(signedRx.ValidUntilUtc);

        // 3. Attempting to update a signed prescription returns 409 Conflict (Immutability guarantee)
        var updateReq = new UpdatePrescriptionDraftRequest
        {
            ExpectedVersion = signedRx.Version,
            DiagnosisSummary = "Değiştirilmeye çalışılan tanı",
            Items = [],
        };
        var updateResp = await PutWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/pharmacy/prescriptions/{rxId}",
            updateReq);

        Assert.Equal(HttpStatusCode.Conflict, updateResp.StatusCode);

        // 4. Pharmacist views prescription
        var pharmacistClient = CreateSecureClient(application);
        var pharmLogin = await LoginAsync(pharmacistClient, "DEMO-pharmacist@hospital.invalid", "DEMO-Pharm-Pass!1");
        Assert.Equal(HttpStatusCode.OK, pharmLogin.StatusCode);

        var pharmViewResp = await pharmacistClient.GetAsync($"/api/v1/pharmacy/prescriptions/{rxId}");
        Assert.Equal(HttpStatusCode.OK, pharmViewResp.StatusCode);
        var pharmRx = await pharmViewResp.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(pharmRx);
        Assert.Equal(rxId, pharmRx.Id);

        // 5. Patient views own prescriptions
        var patientClient = CreateSecureClient(application);
        var patLogin = await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");
        Assert.Equal(HttpStatusCode.OK, patLogin.StatusCode);

        var patientRxListResp = await patientClient.GetAsync($"/api/v1/pharmacy/prescriptions/by-patient/{patientPersonId}");
        Assert.Equal(HttpStatusCode.OK, patientRxListResp.StatusCode);
        var patientRxs = await patientRxListResp.Content.ReadFromJsonAsync<List<PrescriptionSummaryResponse>>();
        Assert.NotNull(patientRxs);
        Assert.Contains(patientRxs, p => p.Id == rxId && p.Status == "Signed");

        // 6. Doctor cancels prescription with reason
        var cancelReq = new CancelPrescriptionRequest
        {
            ExpectedVersion = signedRx.Version,
            Reason = "Klinik endikasyon değişikliği ve alerji şüphesi.",
        };
        var cancelResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/pharmacy/prescriptions/{rxId}/cancel",
            cancelReq);

        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);
        var cancelledRx = await cancelResp.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(cancelledRx);
        Assert.Equal("Cancelled", cancelledRx.Status);
        Assert.Equal("Klinik endikasyon değişikliği ve alerji şüphesi.", cancelledRx.CancellationReason);

        // 7. Verify Audit Trail in DB
        await using var verifyScope = application.Services.CreateAsyncScope();
        var auditDb = verifyScope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var auditLogs = await auditDb.AuditLogs.AsNoTracking().ToListAsync();

        Assert.Contains(auditLogs, a => a.Action == "Pharmacy.PrescriptionCreateDraft" && a.TargetResourceId == rxId.ToString());
        Assert.Contains(auditLogs, a => a.Action == "Pharmacy.PrescriptionSign" && a.TargetResourceId == rxId.ToString());
        Assert.Contains(auditLogs, a => a.Action == "Pharmacy.PrescriptionCancel" && a.TargetResourceId == rxId.ToString());
        Assert.DoesNotContain(auditLogs, a => a.Reason?.Contains("alerji şüphesi", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G02")]
    public async Task PrescriptionSecurityAndIdorProtection()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        // 1. Unauthenticated request -> 401
        var unauthClient = application.CreateClient();
        var unauthResp = await unauthClient.GetAsync($"/api/v1/pharmacy/prescriptions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthResp.StatusCode);

        // 2. Patient cannot create prescription -> 403
        var patientClient = CreateSecureClient(application);
        var patLogin = await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");
        Assert.Equal(HttpStatusCode.OK, patLogin.StatusCode);

        var patientCreateResp = await PostWithAntiforgeryAsync(
            patientClient,
            "/api/v1/pharmacy/prescriptions",
            new CreatePrescriptionDraftRequest
            {
                PatientId = Guid.Parse("00000000-0000-0000-0000-000000000109"),
                EncounterId = Guid.NewGuid(),
                DepartmentId = Guid.NewGuid(),
            });

        Assert.Equal(HttpStatusCode.Forbidden, patientCreateResp.StatusCode);

        // 3. Patient cannot view another patient's prescriptions -> 403
        var anotherPatientId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var idorResp = await patientClient.GetAsync($"/api/v1/pharmacy/prescriptions/by-patient/{anotherPatientId}");
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

        var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
        await identitySeeder.SeedAsync();

        var pharmacySeeder = scope.ServiceProvider.GetRequiredService<IMedicationCatalogDataSeeder>();
        await pharmacySeeder.SeedAsync();
    }
}
