using System.Net;
using System.Net.Http.Json;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Pharmacy;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Domain;
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

public sealed class MedicationSafetyIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G04")]
    public async Task SafetyCheckEndpointReturnsAllergyAndDuplicateWarnings()
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

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var doctorId = Guid.NewGuid();
        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(application);

        // 1. Add active Penicillin allergy for patient in ClinicalRecordsDb
        using (var scope = application.Services.CreateScope())
        {
            var clinicalDb = scope.ServiceProvider.GetRequiredService<ClinicalRecordsDbContext>();
            var allergy = AllergyIntolerance.Create(
                Guid.NewGuid(),
                patientId,
                null,
                "Penisilin",
                AllergyCategory.Medication,
                AllergyCriticality.High,
                "Döküntü ve solunum güçlüğü",
                DateTime.UtcNow.AddYears(-1),
                "Ciddi alerji öyküsü",
                doctorId,
                DateTime.UtcNow);

            clinicalDb.AllergyIntolerances.Add(allergy);
            await clinicalDb.SaveChangesAsync();
        }

        // 2. Fetch catalog item IDs for Amoxicillin (DEMO-MED-AMX500)
        Guid amoxId;
        using (var scope = application.Services.CreateScope())
        {
            var pharmacyDb = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var amox = await pharmacyDb.MedicationCatalogItems.FirstAsync(m => m.Code == "DEMO-MED-AMX500");
            amoxId = amox.Id;
        }

        // 3. Call safety check endpoint
        var safetyReq = new MedicationSafetyCheckRequest
        {
            PatientId = patientId,
            EncounterId = encounterId,
            Items =
            [
                new MedicationSafetyItemRequest
                {
                    MedicationCatalogItemId = amoxId,
                    Dose = 500,
                    DoseUnit = "mg",
                    Frequency = "2x1",
                    DurationDays = 7,
                },
            ],
        };

        var response = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/pharmacy/prescriptions/safety-check",
            safetyReq);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MedicationSafetyCheckResponse>();
        Assert.NotNull(result);
        Assert.True(result.HasWarnings);
        Assert.True(result.HasCriticalWarnings);
        Assert.Contains(result.Warnings, w => w.WarningType == "AllergyCrossReaction");
        Assert.Contains("sentetik DEMO verilerle", result.Disclaimer, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G04")]
    public async Task SignPrescriptionWithCriticalWarningFailsWithoutOverrideReasonAndSucceedsWithOverrideReason()
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

        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(application);
        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var deptId = Guid.Parse("30000000-0000-0000-0000-000000000003");

        // 1. Fetch catalog items for Aspirin and Ibuprofen (critical interaction)
        Guid aspId;
        Guid ibuId;
        using (var scope = application.Services.CreateScope())
        {
            var pharmacyDb = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var asp = await pharmacyDb.MedicationCatalogItems.FirstAsync(m => m.Code == "DEMO-MED-ASA100");
            var ibu = await pharmacyDb.MedicationCatalogItems.FirstAsync(m => m.Code == "DEMO-MED-IBU400");
            aspId = asp.Id;
            ibuId = ibu.Id;
        }

        // 2. Create Prescription Draft with both Aspirin and Ibuprofen
        var draftReq = new CreatePrescriptionDraftRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            DepartmentId = deptId,
            DiagnosisSummary = "Eklem Ağrısı ve Kardiyak Takip",
            GeneralInstructions = "Tok karnına",
            Items =
            [
                new CreatePrescriptionDraftItemRequest
                {
                    MedicationCatalogItemId = aspId,
                    Dose = 100,
                    DoseUnit = "mg",
                    Frequency = "1x1",
                    DurationDays = 30,
                    Quantity = 1,
                    QuantityUnit = "kutu",
                },
                new CreatePrescriptionDraftItemRequest
                {
                    MedicationCatalogItemId = ibuId,
                    Dose = 400,
                    DoseUnit = "mg",
                    Frequency = "2x1",
                    DurationDays = 7,
                    Quantity = 1,
                    QuantityUnit = "kutu",
                },
            ],
        };

        var draftRes = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/pharmacy/prescriptions",
            draftReq);

        Assert.Equal(HttpStatusCode.Created, draftRes.StatusCode);
        var draft = await draftRes.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(draft);

        // 3. Try to sign WITHOUT override reason -> should fail with 422 UnprocessableEntity
        var signFailRes = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/pharmacy/prescriptions/{draft.Id}/sign",
            new SignPrescriptionRequest
            {
                ExpectedVersion = draft.Version,
                ValidDays = 14,
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, signFailRes.StatusCode);

        // 4. Sign WITH valid override reason -> should succeed with 200 OK
        var signSuccessRes = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/pharmacy/prescriptions/{draft.Id}/sign",
            new SignPrescriptionRequest
            {
                ExpectedVersion = draft.Version,
                ValidDays = 14,
                OverrideReason = "Klinik fayda-risk analizi yapıldı, hasta gastrointestinal kanama semptomları açısından bilgilendirildi.",
                AcknowledgedWarningCodes = ["DEMO-INT-ASP-IBU"],
            });

        Assert.Equal(HttpStatusCode.OK, signSuccessRes.StatusCode);
        var signed = await signSuccessRes.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(signed);
        Assert.Equal("Signed", signed.Status);

        // 5. Verify audit log entry for Override
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var auditLogs = await auditDb.AuditLogs.AsNoTracking().ToListAsync();
            var overrideLog = auditLogs.FirstOrDefault(l => l.Action == "Pharmacy.PrescriptionSafetyWarningOverride" && l.TargetResourceId == draft.Id.ToString());

            Assert.NotNull(overrideLog);
            Assert.DoesNotContain("gastrointestinal", overrideLog.Reason, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("açık onayla", overrideLog.Reason, StringComparison.OrdinalIgnoreCase);
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

        var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
        await identitySeeder.SeedAsync();

        var pharmacySeeder = scope.ServiceProvider.GetRequiredService<IMedicationCatalogDataSeeder>();
        await pharmacySeeder.SeedAsync();
    }
}
