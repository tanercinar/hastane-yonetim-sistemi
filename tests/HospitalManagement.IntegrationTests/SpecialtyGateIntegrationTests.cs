using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Specialty;
using HospitalManagement.Host.Authorization;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Domain;
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
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class SpecialtyGateIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F09-KAPI")]
    public async Task Phase9SpecialtyGateCompleteVerificationSucceeds()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000201");
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var midwifeId = Guid.Parse("00000000-0000-0000-0000-000000000103");
        var encounterId = await ClinicalTestData.SeedAppointmentLinkedStartedEncounterAsync(application, patientId);
        var homeHealthEncounterId = await ClinicalTestData.SeedStartedEncounterAsync(
            application,
            patientId,
            encounterType: EncounterType.HomeHealth);

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // --- 1. OBSTETRICS VERTICAL FLOW ---
        // 1.1 Create Pregnancy Episode
        var pregReq = new CreatePregnancyEpisodeRequest
        {
            PatientId = patientId,
            OpeningEncounterId = encounterId,
            LastMenstrualPeriodUtc = DateTime.UtcNow.AddDays(-150),
            Gravida = 1,
            Para = 0,
            BloodGroupAndRh = "0+",
            RiskCategory = "LowRisk",
            AssignedDoctorId = doctorId,
            AssignedMidwifeId = midwifeId,
        };
        var pregResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/pregnancy-episodes", pregReq);
        Assert.Equal(HttpStatusCode.Created, pregResp.StatusCode);
        var episode = await pregResp.Content.ReadFromJsonAsync<PregnancyEpisodeResponse>();
        Assert.NotNull(episode);

        // 1.2 Record Antenatal Visit
        var visitReq = new RecordAntenatalVisitRequest
        {
            EncounterId = encounterId,
            GestationalAgeWeeks = 21,
            GestationalAgeDays = 3,
            MaternalWeightKg = 64.5m,
            SystolicBpMmHg = 115,
            DiastolicBpMmHg = 75,
            FundalHeightCm = 20.5m,
            FetalHeartRateBpm = 144,
            FetalPresentation = "Cephalic",
            EdemaLevel = "None",
            UrineProteinPresent = false,
            UrineGlucosePresent = false,
            ClinicalNotes = "Fetal hareketler hissediliyor, rutin takip normal",
        };
        var visitResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/specialty/pregnancy-episodes/{episode.Id}/antenatal-visits", visitReq);
        Assert.Equal(HttpStatusCode.Created, visitResp.StatusCode);

        // 1.3 Create Delivery Record (Spontaneous Vaginal) & Newborn
        var newbornPatientId = await SpecialtyTestData.SeedNewbornPatientAsync(
            application,
            DateOnly.FromDateTime(DateTime.UtcNow),
            HospitalManagement.Modules.Patients.Domain.Gender.Male);
        var delReq = new CreateDeliveryRecordRequest
        {
            PregnancyEpisodeId = episode.Id,
            MotherPatientId = patientId,
            DeliveryTimeUtc = DateTime.UtcNow,
            DeliveryMode = "SpontaneousVaginal",
            AttendingDoctorId = doctorId,
            AssistingMidwifeId = midwifeId,
            GestationalAgeWeeks = 39,
            GestationalAgeDays = 4,
            EstimatedBloodLossMl = 300,
            PerinealTear = "None",
            Newborns =
            [
                new AddNewbornRequest
                {
                    NewbornPatientId = newbornPatientId,
                    BirthOrder = 1,
                    BirthTimeUtc = DateTime.UtcNow,
                    Gender = "Male",
                    BirthWeightGrams = 3500,
                    BirthLengthCm = 51.0m,
                    HeadCircumferenceCm = 35.5m,
                    ApgarScore1Min = 9,
                    ApgarScore5Min = 10,
                }
            ]
        };
        var delResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/deliveries", delReq);
        Assert.Equal(HttpStatusCode.Created, delResp.StatusCode);

        // Verify Episode automatically transitioned to Delivered
        var episodeAfterDel = await doctorClient.GetFromJsonAsync<PregnancyEpisodeResponse>($"/api/v1/specialty/pregnancy-episodes/{episode.Id}");
        Assert.NotNull(episodeAfterDel);
        Assert.Equal("Delivered", episodeAfterDel.Status);

        // Negative check: Cannot add visit to delivered episode
        var negVisitResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/specialty/pregnancy-episodes/{episode.Id}/antenatal-visits", visitReq);
        Assert.Equal(HttpStatusCode.Conflict, negVisitResp.StatusCode);


        // --- 2. ODONTOLOGY VERTICAL FLOW ---
        // 2.1 Examination
        var examReq = new CreateDentalExaminationRequest
        {
            PatientId = patientId,
            DentistId = doctorId,
            ExaminationDateUtc = DateTime.UtcNow,
            ChiefComplaint = "16 numaralı dişte çürük ve soğuk hassasiyeti",
            DiagnosisNotes = "Oklüzal derin kavite",
            TreatmentPlanSummary = "16 dolgu planlandı",
        };
        var examResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/dental/examinations", examReq);
        Assert.Equal(HttpStatusCode.Created, examResp.StatusCode);

        // 2.2 Record Tooth Condition (v1: Caries)
        var toothReq = new RecordToothConditionRequest
        {
            ToothNumber = 16,
            Condition = "Caries",
            AffectedSurfaces = 4, // Occlusal
            Notes = "Oklüzal kavite",
        };
        var tooth1Resp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/specialty/dental/odontogram/{patientId}/tooth", toothReq);
        Assert.Equal(HttpStatusCode.Created, tooth1Resp.StatusCode);

        // 2.3 Plan & Complete Procedure
        var procReq = new PlanDentalProcedureRequest
        {
            PatientId = patientId,
            ToothNumber = 16,
            Surfaces = 4,
            ProcedureCode = "DNT-FILLING",
            ProcedureName = "Kompozit Dolgu",
            EstimatedCost = 750,
            PerformedByDoctorId = doctorId,
            ScheduledDateUtc = DateTime.UtcNow.AddDays(1),
        };
        var procResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/dental/procedures", procReq);
        Assert.Equal(HttpStatusCode.Created, procResp.StatusCode);
        var proc = await procResp.Content.ReadFromJsonAsync<DentalProcedureResponse>();
        Assert.NotNull(proc);

        var compProcReq = new CompleteDentalProcedureRequest
        {
            CompletedDateUtc = DateTime.UtcNow,
            CompletionNotes = "Dolgu tamamlandı.",
        };
        var compProcResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/specialty/dental/procedures/{proc.Id}/complete", compProcReq);
        Assert.Equal(HttpStatusCode.OK, compProcResp.StatusCode);

        var unpublishedProcReq = new PlanDentalProcedureRequest
        {
            PatientId = patientId,
            ToothNumber = 26,
            Surfaces = 4,
            ProcedureCode = "DEMO-DRAFT-HIDDEN",
            ProcedureName = "DEMO-GIZLI-TASLAK-ISLEM",
            EstimatedCost = 900,
            PerformedByDoctorId = doctorId,
            ClinicalNotes = "DEMO-GIZLI-KLINIK-NOT",
        };
        var unpublishedProcResp = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/specialty/dental/procedures",
            unpublishedProcReq);
        Assert.Equal(HttpStatusCode.Created, unpublishedProcResp.StatusCode);

        // 2.4 Update Tooth Condition to Filled (v2) - Non-destructive history check
        var toothReq2 = new RecordToothConditionRequest
        {
            ToothNumber = 16,
            Condition = "Filled",
            AffectedSurfaces = 4,
            Notes = "Dolgu sağlam",
        };
        var tooth2Resp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/specialty/dental/odontogram/{patientId}/tooth", toothReq2);
        Assert.Equal(HttpStatusCode.Created, tooth2Resp.StatusCode);

        var history = await doctorClient.GetFromJsonAsync<List<ToothConditionResponse>>($"/api/v1/specialty/dental/odontogram/{patientId}/tooth/16/history");
        Assert.NotNull(history);
        Assert.Equal(2, history.Count);
        Assert.Equal(2, history[0].Version);
        Assert.Equal(1, history[1].Version);


        // --- 3. HOME HEALTH VERTICAL FLOW ---
        // 3.1 Request Visit
        var homeReq = new RequestHomeHealthVisitRequest
        {
            PatientId = patientId,
            ServiceType = "WoundDressing",
            Priority = "Urgent",
            City = "İstanbul",
            District = "Kadıköy",
            AddressDetail = "Caferağa Mah. No: 1",
            ContactPhone = "05320000000",
            InitialNotes = "Yara pansumanı",
        };
        var homeResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/home-health/visits", homeReq);
        Assert.Equal(HttpStatusCode.Created, homeResp.StatusCode);
        var homeVisit = await homeResp.Content.ReadFromJsonAsync<HomeHealthVisitResponse>();
        Assert.NotNull(homeVisit);

        // 3.2 Assign Team
        var assignReq = new AssignHomeHealthTeamRequest
        {
            AssignedStaffId = midwifeId,
            ScheduledDateUtc = DateTime.UtcNow.AddHours(2),
        };
        var assignResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/specialty/home-health/visits/{homeVisit.Id}/assign", assignReq);
        Assert.Equal(HttpStatusCode.OK, assignResp.StatusCode);

        // 3.3 Start Visit
        var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        var startResp = await PostWithAntiforgeryAsync<object?>(nurseClient, $"/api/v1/specialty/home-health/visits/{homeVisit.Id}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);

        // 3.4 Complete Visit
        var compHomeReq = new CompleteHomeHealthVisitRequest
        {
            ClinicalNotes = "Pansuman tamamlandı.",
            VitalsSummaryNotes = "TA: 120/80 mmHg",
            EncounterId = homeHealthEncounterId,
        };
        var compHomeResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/specialty/home-health/visits/{homeVisit.Id}/complete", compHomeReq);
        Assert.Equal(HttpStatusCode.OK, compHomeResp.StatusCode);

        // --- 4. PATIENT PORTAL PUBLICATION & IDOR SHIELDING ---
        var patientClient = CreateSecureClient(application);
        var patientLogin = await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");
        Assert.Equal(HttpStatusCode.OK, patientLogin.StatusCode);

        var portalResponse = await patientClient.GetAsync(
            "/api/v1/specialty/patient-portal/my-records?patientId=00000000-0000-0000-0000-000000000202");
        Assert.Equal(HttpStatusCode.OK, portalResponse.StatusCode);
        var portal = await portalResponse.Content.ReadFromJsonAsync<PatientSpecialtyPortalResponse>();
        Assert.NotNull(portal);
        Assert.Single(portal.Pregnancies);
        Assert.Single(portal.Deliveries);
        Assert.Equal(1, portal.Deliveries[0].NewbornCount);
        Assert.Single(portal.DentalExaminations);
        Assert.Single(portal.DentalProcedures);
        Assert.Equal("Completed", portal.DentalProcedures[0].Status);
        Assert.Single(portal.HomeHealthVisits);

        var portalJson = await portalResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("DEMO-GIZLI-TASLAK-ISLEM", portalJson, StringComparison.Ordinal);
        Assert.DoesNotContain("DEMO-GIZLI-KLINIK-NOT", portalJson, StringComparison.Ordinal);
        Assert.DoesNotContain("Caferağa Mah. No: 1", portalJson, StringComparison.Ordinal);
        Assert.DoesNotContain("05320000000", portalJson, StringComparison.Ordinal);
        Assert.DoesNotContain(newbornPatientId.ToString("D"), portalJson, StringComparison.OrdinalIgnoreCase);

        using (var auditScope = application.Services.CreateScope())
        {
            var auditDb = auditScope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var portalAudit = await auditDb.AuditLogs
                .AsNoTracking()
                .SingleAsync(entry => entry.Action == "Specialty.PatientPortalView");
            Assert.Equal("PatientSpecialtyPortal", portalAudit.TargetResourceType);
            Assert.Equal(patientId.ToString("D"), portalAudit.TargetResourceId);
            Assert.Null(portalAudit.DetailsJson);
        }

        var doctorPortalResponse = await doctorClient.GetAsync("/api/v1/specialty/patient-portal/my-records");
        Assert.Equal(HttpStatusCode.Forbidden, doctorPortalResponse.StatusCode);

        // --- 5. OPERATIONAL REPORTING & SENSITIVE DATA SHIELDING ---
        var managerClient = CreateSecureClient(application);
        var managerLogin = await LoginAsync(managerClient, "DEMO-manager@hospital.invalid", "DEMO-Manager-Pass!1");
        Assert.Equal(HttpStatusCode.OK, managerLogin.StatusCode);

        var summaryResp = await managerClient.GetAsync("/api/v1/specialty/reports/operational-summary");
        Assert.Equal(HttpStatusCode.OK, summaryResp.StatusCode);
        var summary = await summaryResp.Content.ReadFromJsonAsync<SpecialtyOperationalSummaryResponse>();
        Assert.NotNull(summary);
        Assert.Equal(1, summary.TotalDeliveriesCount);
        Assert.Equal(2, summary.TotalDentalProceduresCount);
        Assert.Equal(1, summary.CompletedDentalProceduresCount);
        Assert.Equal(1, summary.CompletedHomeVisitsCount);


        // --- 6. AUTHORIZATION & NEGATIVE PATHS ---
        var adminClient = CreateSecureClient(application);
        var adminLogin = await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1");
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        // SystemAdministrator cannot write clinical records
        var adminPregResp = await PostWithAntiforgeryAsync(adminClient, "/api/v1/specialty/pregnancy-episodes", pregReq);
        Assert.Equal(HttpStatusCode.Forbidden, adminPregResp.StatusCode);

        var adminToothResp = await PostWithAntiforgeryAsync(adminClient, $"/api/v1/specialty/dental/odontogram/{patientId}/tooth", toothReq);
        Assert.Equal(HttpStatusCode.Forbidden, adminToothResp.StatusCode);

        var adminHomeResp = await PostWithAntiforgeryAsync(adminClient, "/api/v1/specialty/home-health/visits", homeReq);
        Assert.Equal(HttpStatusCode.Forbidden, adminHomeResp.StatusCode);

        // Technical administrators cannot read clinical/operational report data.
        var adminReportResp = await adminClient.GetAsync("/api/v1/specialty/reports/operational-summary");
        Assert.Equal(HttpStatusCode.Forbidden, adminReportResp.StatusCode);

        var adminPortalResp = await adminClient.GetAsync("/api/v1/specialty/patient-portal/my-records");
        Assert.Equal(HttpStatusCode.Forbidden, adminPortalResp.StatusCode);

        using var anonymousClient = CreateSecureClient(application);
        var anonymousPortalResp = await anonymousClient.GetAsync("/api/v1/specialty/patient-portal/my-records");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousPortalResp.StatusCode);
    }

    private static HttpClient CreateSecureClient(WebApplicationFactory<Program> application)
    {
        return application.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password)
    {
        var antiforgeryResp = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/v1/identity/antiforgery");
        Assert.NotNull(antiforgeryResp);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/sessions")
        {
            Content = JsonContent.Create(new LoginRequest
            {
                Email = email,
                Password = password,
            }),
        };
        request.Headers.Add("X-HMS-CSRF", antiforgeryResp.Token);

        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostWithAntiforgeryAsync<T>(
        HttpClient client,
        string url,
        T body)
    {
        var antiforgeryResp = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/v1/identity/antiforgery");
        Assert.NotNull(antiforgeryResp);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = body is not null ? JsonContent.Create(body) : null,
        };
        request.Headers.Add("X-HMS-CSRF", antiforgeryResp.Token);

        return await client.SendAsync(request);
    }

    private static async Task RunAllMigrationsAndSeedAsync(WebApplicationFactory<Program> application)
    {
        using var scope = application.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var idDb = sp.GetRequiredService<IdentityAccessDbContext>();
        await idDb.Database.MigrateAsync();

        var audDb = sp.GetRequiredService<AuditPrivacyDbContext>();
        await audDb.Database.MigrateAsync();

        var orgDb = sp.GetRequiredService<OrganizationDbContext>();
        await orgDb.Database.MigrateAsync();

        var patDb = sp.GetRequiredService<PatientsDbContext>();
        await patDb.Database.MigrateAsync();

        var schDb = sp.GetRequiredService<SchedulingDbContext>();
        await schDb.Database.MigrateAsync();

        var notDb = sp.GetRequiredService<NotificationsDbContext>();
        await notDb.Database.MigrateAsync();

        var clinDb = sp.GetRequiredService<ClinicalRecordsDbContext>();
        await clinDb.Database.MigrateAsync();

        var pharmDb = sp.GetRequiredService<PharmacyDbContext>();
        await pharmDb.Database.MigrateAsync();

        var diagDb = sp.GetRequiredService<DiagnosticsDbContext>();
        await diagDb.Database.MigrateAsync();

        var inpDb = sp.GetRequiredService<InpatientDbContext>();
        await inpDb.Database.MigrateAsync();

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
