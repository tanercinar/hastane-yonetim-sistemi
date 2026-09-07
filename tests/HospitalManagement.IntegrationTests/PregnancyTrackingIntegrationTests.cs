using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Specialty;
using HospitalManagement.Host.Authorization;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Application;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;
using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;
using HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class PregnancyTrackingIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F09-G01")]
    public async Task PregnancyEpisodeLifecycleVisitsRiskUpdateAndCompletionSucceeds()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000201");
        var encounterId = await ClinicalTestData.SeedAppointmentLinkedStartedEncounterAsync(application, patientId);
        var appointmentlessEncounterId = await ClinicalTestData.SeedStartedEncounterAsync(application, patientId);
        var otherPatientEncounterId = await ClinicalTestData.SeedAppointmentLinkedStartedEncounterAsync(
            application,
            Guid.Parse("00000000-0000-0000-0000-000000000202"));
        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // 1. Create Pregnancy Episode
        var lmp = DateTime.UtcNow.AddDays(-140); // 20 weeks ago
        var missingEncounterResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/specialty/pregnancy-episodes",
            new CreatePregnancyEpisodeRequest
            {
                PatientId = patientId,
                Gravida = 1,
                LastMenstrualPeriodUtc = lmp,
                RiskCategory = "LowRisk",
            });
        Assert.Equal(HttpStatusCode.BadRequest, missingEncounterResponse.StatusCode);

        foreach (var invalidEncounterId in new[] { appointmentlessEncounterId, otherPatientEncounterId })
        {
            var invalidEncounterResponse = await PostWithAntiforgeryAsync(
                doctorClient,
                "/api/v1/specialty/pregnancy-episodes",
                new CreatePregnancyEpisodeRequest
                {
                    PatientId = patientId,
                    OpeningEncounterId = invalidEncounterId,
                    Gravida = 1,
                    LastMenstrualPeriodUtc = lmp,
                    RiskCategory = "LowRisk",
                });
            Assert.Equal(HttpStatusCode.BadRequest, invalidEncounterResponse.StatusCode);
        }

        var createReq = new CreatePregnancyEpisodeRequest
        {
            PatientId = patientId,
            OpeningEncounterId = encounterId,
            Gravida = 2,
            Para = 1,
            Abortus = 0,
            LivingChildren = 1,
            LastMenstrualPeriodUtc = lmp,
            BloodGroupAndRh = "A Rh(+)",
            RiskCategory = "LowRisk",
            RiskFactorsNotes = "Standart antenatal izlem",
        };

        var createResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/pregnancy-episodes", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var episode = await createResp.Content.ReadFromJsonAsync<PregnancyEpisodeResponse>();
        Assert.NotNull(episode);
        Assert.Equal(encounterId, episode.OpeningEncounterId);
        Assert.Equal("Active", episode.Status);
        Assert.Equal(lmp.AddDays(280), episode.EstimatedDeliveryDateUtc);

        // 2. Record 1st Visit (20th week)
        var visit1Req = new RecordAntenatalVisitRequest
        {
            EncounterId = encounterId,
            VisitDateUtc = DateTime.UtcNow,
            GestationalAgeWeeks = 20,
            GestationalAgeDays = 0,
            MaternalWeightKg = 65.5m,
            SystolicBpMmHg = 110,
            DiastolicBpMmHg = 70,
            FundalHeightCm = 20.0m,
            FetalHeartRateBpm = 145,
            FetalPresentation = "Cephalic",
            EdemaLevel = "None",
            UrineProteinPresent = false,
            UrineGlucosePresent = false,
            ClinicalNotes = "Fetal hareketler ve gelişim normal",
            NextVisitRecommendedDateUtc = DateTime.UtcNow.AddDays(28),
        };

        var visit1Resp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/specialty/pregnancy-episodes/{episode.Id}/antenatal-visits",
            visit1Req);
        Assert.Equal(HttpStatusCode.Created, visit1Resp.StatusCode);
        var visit1 = await visit1Resp.Content.ReadFromJsonAsync<AntenatalVisitResponse>();
        Assert.NotNull(visit1);
        Assert.Equal(encounterId, visit1.EncounterId);
        Assert.Equal(20, visit1.GestationalAgeWeeks);

        // 3. Update Risk to GestationalDiabetes
        var riskReq = new UpdatePregnancyRiskCategoryRequest
        {
            RiskCategory = "GestationalDiabetes",
            RiskFactorsNotes = "24. hafta OGTT 50g yüksek çıktı, diyete başlandı",
        };

        var riskResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/specialty/pregnancy-episodes/{episode.Id}/risk-category",
            riskReq);
        Assert.Equal(HttpStatusCode.OK, riskResp.StatusCode);
        var updatedRiskEp = await riskResp.Content.ReadFromJsonAsync<PregnancyEpisodeResponse>();
        Assert.NotNull(updatedRiskEp);
        Assert.Equal("GestationalDiabetes", updatedRiskEp.RiskCategory);

        // 4. Duplicate Active Episode returns 409 Conflict
        var dupResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/pregnancy-episodes", createReq);
        Assert.Equal(HttpStatusCode.Conflict, dupResp.StatusCode);

        // 5. Complete Episode
        var completeReq = new CompletePregnancyEpisodeRequest
        {
            OutcomeStatus = "Delivered",
        };

        var completeResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/specialty/pregnancy-episodes/{episode.Id}/complete",
            completeReq);
        Assert.Equal(HttpStatusCode.OK, completeResp.StatusCode);
        var completedEp = await completeResp.Content.ReadFromJsonAsync<PregnancyEpisodeResponse>();
        Assert.NotNull(completedEp);
        Assert.Equal("Delivered", completedEp.Status);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F09-G01")]
    public async Task SystemAdministratorCannotCreatePregnancyEpisodes()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var adminClient = CreateSecureClient(application);
        var adminLogin = await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1");
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        var req = new CreatePregnancyEpisodeRequest
        {
            PatientId = Guid.Parse("00000000-0000-0000-0000-000000000201"),
            Gravida = 1,
            LastMenstrualPeriodUtc = DateTime.UtcNow.AddDays(-60),
        };

        var resp = await PostWithAntiforgeryAsync(adminClient, "/api/v1/specialty/pregnancy-episodes", req);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F09-REVIEW")]
    public async Task SpecialtyEndpointsEnforceAuthenticationCareRelationshipAndStrictEnums()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var relatedPatientId = Guid.Parse("00000000-0000-0000-0000-000000000201");
        var unrelatedPatientId = Guid.Parse("00000000-0000-0000-0000-000000000202");
        var encounterId = await ClinicalTestData.SeedAppointmentLinkedStartedEncounterAsync(application, relatedPatientId);

        var anonymousClient = CreateSecureClient(application);
        var anonymousResponse = await anonymousClient.GetAsync(
            $"/api/v1/specialty/pregnancy-episodes/patient/{relatedPatientId}");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var doctorClient = CreateSecureClient(application);
        var login = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var unrelatedRead = await doctorClient.GetAsync(
            $"/api/v1/specialty/pregnancy-episodes/patient/{unrelatedPatientId}");
        Assert.Equal(HttpStatusCode.Forbidden, unrelatedRead.StatusCode);

        var unrelatedCreate = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/specialty/pregnancy-episodes",
            new CreatePregnancyEpisodeRequest
            {
                PatientId = unrelatedPatientId,
                Gravida = 1,
                LastMenstrualPeriodUtc = DateTime.UtcNow.AddDays(-70),
                RiskCategory = "LowRisk",
            });
        Assert.Equal(HttpStatusCode.Forbidden, unrelatedCreate.StatusCode);

        const string auditCanary = "F09-SENSITIVE-CANARY";
        var invalidEnumCreate = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/specialty/pregnancy-episodes",
            new CreatePregnancyEpisodeRequest
            {
                PatientId = relatedPatientId,
                OpeningEncounterId = encounterId,
                Gravida = 1,
                LastMenstrualPeriodUtc = DateTime.UtcNow.AddDays(-70),
                RiskCategory = "not-a-clinical-enum",
                RiskFactorsNotes = auditCanary,
            });
        Assert.Equal(HttpStatusCode.BadRequest, invalidEnumCreate.StatusCode);

        var invalidTeamCreate = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/specialty/pregnancy-episodes",
            new CreatePregnancyEpisodeRequest
            {
                PatientId = relatedPatientId,
                OpeningEncounterId = encounterId,
                Gravida = 1,
                LastMenstrualPeriodUtc = DateTime.UtcNow.AddDays(-70),
                RiskCategory = "LowRisk",
                AssignedDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000103"),
            });
        Assert.Equal(HttpStatusCode.BadRequest, invalidTeamCreate.StatusCode);

        var validCreate = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/specialty/pregnancy-episodes",
            new CreatePregnancyEpisodeRequest
            {
                PatientId = relatedPatientId,
                OpeningEncounterId = encounterId,
                Gravida = 1,
                LastMenstrualPeriodUtc = DateTime.UtcNow.AddDays(-70),
                RiskCategory = "LowRisk",
                RiskFactorsNotes = auditCanary,
            });
        Assert.Equal(HttpStatusCode.Created, validCreate.StatusCode);

        using var scope = application.Services.CreateScope();
        var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var auditPayloads = await auditDb.AuditLogs
            .AsNoTracking()
            .Select(entry => new { entry.Reason, entry.DetailsJson })
            .ToListAsync();

        Assert.DoesNotContain(
            auditPayloads,
            entry => (entry.Reason?.Contains(auditCanary, StringComparison.Ordinal) ?? false)
                || (entry.DetailsJson?.Contains(auditCanary, StringComparison.Ordinal) ?? false));

        var relatedEpisodes = await doctorClient.GetFromJsonAsync<List<PregnancyEpisodeResponse>>(
            $"/api/v1/specialty/pregnancy-episodes/patient/{relatedPatientId}");
        Assert.NotNull(relatedEpisodes);
        Assert.Single(relatedEpisodes);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F09-REVIEW")]
    public async Task DatabaseConstraintsRejectConcurrentSpecialtyRecordVersions()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000201");
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var now = DateTime.UtcNow;

        static PregnancyEpisode NewEpisode(Guid patientId, DateTime now) => PregnancyEpisode.Create(
            Guid.NewGuid(),
            patientId,
            Guid.NewGuid(),
            gravida: 1,
            para: 0,
            abortus: 0,
            livingChildren: 0,
            lastMenstrualPeriodUtc: now.AddDays(-70),
            customEstimatedDeliveryDateUtc: null,
            bloodGroupAndRh: null,
            riskCategory: PregnancyRiskCategory.LowRisk,
            riskFactorsNotes: null,
            assignedDoctorId: null,
            assignedMidwifeId: null,
            nowUtc: now);

        using (var firstScope = application.Services.CreateScope())
        {
            var db = firstScope.ServiceProvider.GetRequiredService<SpecialtyCareDbContext>();
            db.PregnancyEpisodes.Add(NewEpisode(patientId, now));
            await db.SaveChangesAsync();
        }

        using (var duplicateScope = application.Services.CreateScope())
        {
            var db = duplicateScope.ServiceProvider.GetRequiredService<SpecialtyCareDbContext>();
            db.PregnancyEpisodes.Add(NewEpisode(patientId, now.AddSeconds(1)));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        static DentalToothCondition NewToothVersion(Guid patientId, Guid doctorId, DateTime now) =>
            DentalToothCondition.Record(
                Guid.NewGuid(),
                patientId,
                toothNumber: 16,
                condition: ToothCondition.Caries,
                affectedSurfaces: ToothSurface.Occlusal,
                notes: null,
                recordedByStaffId: doctorId,
                version: 1,
                nowUtc: now);

        using (var firstScope = application.Services.CreateScope())
        {
            var db = firstScope.ServiceProvider.GetRequiredService<SpecialtyCareDbContext>();
            db.DentalToothConditions.Add(NewToothVersion(patientId, doctorId, now));
            await db.SaveChangesAsync();
        }

        using (var duplicateScope = application.Services.CreateScope())
        {
            var db = duplicateScope.ServiceProvider.GetRequiredService<SpecialtyCareDbContext>();
            db.DentalToothConditions.Add(NewToothVersion(patientId, doctorId, now.AddSeconds(1)));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }

    private static HttpClient CreateSecureClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost/"),
        });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
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

    private static async Task RunAllMigrationsAndSeedAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var idDb = sp.GetRequiredService<IdentityAccessDbContext>();
        await idDb.Database.MigrateAsync();

        var auditDb = sp.GetRequiredService<AuditPrivacyDbContext>();
        await auditDb.Database.MigrateAsync();

        var patDb = sp.GetRequiredService<PatientsDbContext>();
        await patDb.Database.MigrateAsync();

        var orgDb = sp.GetRequiredService<OrganizationDbContext>();
        await orgDb.Database.MigrateAsync();

        var clinDb = sp.GetRequiredService<ClinicalRecordsDbContext>();
        await clinDb.Database.MigrateAsync();

        var diagDb = sp.GetRequiredService<DiagnosticsDbContext>();
        await diagDb.Database.MigrateAsync();

        var rxDb = sp.GetRequiredService<PharmacyDbContext>();
        await rxDb.Database.MigrateAsync();

        var schedDb = sp.GetRequiredService<SchedulingDbContext>();
        await schedDb.Database.MigrateAsync();

        var inpDb = sp.GetRequiredService<InpatientDbContext>();
        await inpDb.Database.MigrateAsync();

        var notifDb = sp.GetRequiredService<NotificationsDbContext>();
        await notifDb.Database.MigrateAsync();

        var emgDb = sp.GetRequiredService<EmergencyDbContext>();
        await emgDb.Database.MigrateAsync();

        var surgDb = sp.GetRequiredService<SurgeryDbContext>();
        await surgDb.Database.MigrateAsync();

        var specDb = sp.GetRequiredService<SpecialtyCareDbContext>();
        await specDb.Database.MigrateAsync();

        var idSeeder = sp.GetRequiredService<IIdentityDataSeeder>();
        await idSeeder.SeedAsync();

        var orgSeeder = sp.GetRequiredService<IOrganizationDataSeeder>();
        await orgSeeder.SeedAsync();

        var patientSeeder = sp.GetRequiredService<IPatientDataSeeder>();
        await patientSeeder.SeedAsync();

        sp.GetRequiredService<CareRelationshipRegistry>().EstablishCareRelationship(
            Guid.Parse("00000000-0000-0000-0000-000000000102"),
            Guid.Parse("00000000-0000-0000-0000-000000000109"));
    }
}
