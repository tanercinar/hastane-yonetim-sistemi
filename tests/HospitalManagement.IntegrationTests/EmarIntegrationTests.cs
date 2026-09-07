using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Inpatient;
using HospitalManagement.Contracts.Pharmacy;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Application;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class EmarIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F07-G06")]
    public async Task EmarMedicationLifecycleWorksEndToEndWith5RightsAndDoseStates()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var doctorPersonId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var cardDeptId = Guid.Parse("30000000-0000-0000-0000-000000000003");

        // 1. Doctor requests admission
        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(application, patientId);
        var draftResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/pharmacy/prescriptions",
            new CreatePrescriptionDraftRequest
            {
                PatientId = patientId,
                EncounterId = encounterId,
                DepartmentId = cardDeptId,
                Items =
                [
                    new CreatePrescriptionDraftItemRequest
                    {
                        MedicationCatalogItemId = Guid.Parse("00000000-0000-0000-0000-000000000601"),
                        Dose = 500m,
                        DoseUnit = "mg",
                        Frequency = "2x1",
                        DurationDays = 7,
                        Quantity = 1,
                        QuantityUnit = "kutu",
                    },
                ],
            });
        Assert.True(
            draftResponse.StatusCode == HttpStatusCode.Created,
            await draftResponse.Content.ReadAsStringAsync());
        var draft = await draftResponse.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(draft);
        var signResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/pharmacy/prescriptions/{draft.Id}/sign",
            new SignPrescriptionRequest { ExpectedVersion = draft.Version, ValidDays = 7 });
        Assert.Equal(HttpStatusCode.OK, signResponse.StatusCode);
        var signedPrescription = await signResponse.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(signedPrescription);
        var prescribedItem = Assert.Single(signedPrescription.Items);

        var wards = await doctorClient.GetFromJsonAsync<List<WardResponse>>("/api/v1/inpatient/wards");
        Assert.NotNull(wards);
        var cardWard = wards.Single(w => w.Code == "DEMO-WRD-CARD");

        var createAdmissionReq = new CreateAdmissionRequest
        {
            PatientId = patientId,
            EncounterId = encounterId,
            DepartmentId = cardDeptId,
            AdmittingWardId = cardWard.Id,
            AttendingDoctorId = doctorPersonId,
            AdmissionReason = "Akut Koroner Sendrom",
            DietType = "LowSodium",
            FallRiskScore = 40,
            IsolationRequired = "None",
            EstimatedStayDays = 3,
        };

        var admReqResponse = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/admissions", createAdmissionReq);
        Assert.Equal(HttpStatusCode.Created, admReqResponse.StatusCode);
        var admission = await admReqResponse.Content.ReadFromJsonAsync<AdmissionResponse>();
        Assert.NotNull(admission);

        // 2. Nurse accepts and admits patient
        var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        var acceptResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{admission.Id}/accept", new AcceptAdmissionRequest());
        Assert.Equal(HttpStatusCode.OK, acceptResp.StatusCode);

        var availableBeds = await nurseClient.GetFromJsonAsync<List<BedResponse>>($"/api/v1/inpatient/beds?wardId={cardWard.Id}&status=Available");
        Assert.NotNull(availableBeds);
        var bed = availableBeds.First();

        var admitResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{admission.Id}/admit", new AdmitPatientRequest { BedId = bed.Id });
        Assert.Equal(HttpStatusCode.OK, admitResp.StatusCode);

        var visibleAdmissions = await nurseClient.GetFromJsonAsync<List<AdmissionSummaryResponse>>(
            "/api/v1/inpatient/admissions?status=Admitted");
        Assert.NotNull(visibleAdmissions);
        Assert.Contains(visibleAdmissions, item => item.Id == admission.Id);

        var unlinkedScheduleResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            "/api/v1/inpatient/emar/schedule",
            new ScheduleMedicationRequest
            {
                AdmissionId = admission.Id,
                MedicationName = prescribedItem.BrandName,
                Dose = $"{prescribedItem.Dose:0.####} {prescribedItem.DoseUnit}",
                Route = prescribedItem.Route,
                ScheduledTimeUtc = DateTime.UtcNow.AddMinutes(30),
            });
        Assert.Equal(HttpStatusCode.BadRequest, unlinkedScheduleResponse.StatusCode);

        // 3. Nurse schedules Dose 1 (Paracetamol)
        var sched1Req = new ScheduleMedicationRequest
        {
            AdmissionId = admission.Id,
            PrescriptionId = signedPrescription.Id,
            MedicationName = prescribedItem.BrandName,
            Dose = $"{prescribedItem.Dose:0.####} {prescribedItem.DoseUnit}",
            Route = prescribedItem.Route,
            ScheduledTimeUtc = DateTime.UtcNow.AddHours(1),
        };
        var sched1Resp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/inpatient/emar/schedule", sched1Req);
        Assert.Equal(HttpStatusCode.Created, sched1Resp.StatusCode);
        var med1 = await sched1Resp.Content.ReadFromJsonAsync<MedicationAdministrationResponse>();
        Assert.NotNull(med1);
        Assert.Equal("Scheduled", med1.Status);

        // 4. Nurse administers Dose 1 with 5 Rights verification
        var admin1Req = new AdministerMedicationRequest
        {
            Verified5Rights = true,
            Notes = "Hasta oral olarak aldı, tolere etti.",
        };
        var admin1Resp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/emar/{med1.Id}/administer", admin1Req);
        Assert.Equal(HttpStatusCode.OK, admin1Resp.StatusCode);
        var administered1 = await admin1Resp.Content.ReadFromJsonAsync<MedicationAdministrationResponse>();
        Assert.NotNull(administered1);
        Assert.Equal("Administered", administered1.Status);
        Assert.True(administered1.Verified5Rights);

        // 5. Nurse schedules Dose 2 and Skips it
        var sched2Req = new ScheduleMedicationRequest
        {
            AdmissionId = admission.Id,
            PrescriptionId = signedPrescription.Id,
            MedicationName = prescribedItem.BrandName,
            Dose = $"{prescribedItem.Dose:0.####} {prescribedItem.DoseUnit}",
            Route = prescribedItem.Route,
            ScheduledTimeUtc = DateTime.UtcNow.AddHours(2),
        };
        var sched2Resp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/inpatient/emar/schedule", sched2Req);
        Assert.Equal(HttpStatusCode.Created, sched2Resp.StatusCode);
        var med2 = await sched2Resp.Content.ReadFromJsonAsync<MedicationAdministrationResponse>();
        Assert.NotNull(med2);

        var skipResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/emar/{med2.Id}/skip", new SkipMedicationRequest { Reason = "Klinik stabilite nedeniyle atlandı." });
        Assert.Equal(HttpStatusCode.OK, skipResp.StatusCode);
        var skipped = await skipResp.Content.ReadFromJsonAsync<MedicationAdministrationResponse>();
        Assert.NotNull(skipped);
        Assert.Equal("Skipped", skipped.Status);

        // 6. Nurse schedules Dose 3 and patient Refuses
        var sched3Req = new ScheduleMedicationRequest
        {
            AdmissionId = admission.Id,
            PrescriptionId = signedPrescription.Id,
            MedicationName = prescribedItem.BrandName,
            Dose = $"{prescribedItem.Dose:0.####} {prescribedItem.DoseUnit}",
            Route = prescribedItem.Route,
            ScheduledTimeUtc = DateTime.UtcNow.AddHours(3),
        };
        var sched3Resp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/inpatient/emar/schedule", sched3Req);
        Assert.Equal(HttpStatusCode.Created, sched3Resp.StatusCode);
        var med3 = await sched3Resp.Content.ReadFromJsonAsync<MedicationAdministrationResponse>();
        Assert.NotNull(med3);

        var refuseResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/emar/{med3.Id}/refuse", new RefuseMedicationRequest { Reason = "Hasta mide şikayeti nedeniyle reddetti." });
        Assert.Equal(HttpStatusCode.OK, refuseResp.StatusCode);
        var refused = await refuseResp.Content.ReadFromJsonAsync<MedicationAdministrationResponse>();
        Assert.NotNull(refused);
        Assert.Equal("Refused", refused.Status);

        // 7. Nurse schedules Dose 4 and Delays it
        var sched4Req = new ScheduleMedicationRequest
        {
            AdmissionId = admission.Id,
            PrescriptionId = signedPrescription.Id,
            MedicationName = prescribedItem.BrandName,
            Dose = $"{prescribedItem.Dose:0.####} {prescribedItem.DoseUnit}",
            Route = prescribedItem.Route,
            ScheduledTimeUtc = DateTime.UtcNow.AddHours(4),
        };
        var sched4Resp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/inpatient/emar/schedule", sched4Req);
        Assert.Equal(HttpStatusCode.Created, sched4Resp.StatusCode);
        var med4 = await sched4Resp.Content.ReadFromJsonAsync<MedicationAdministrationResponse>();
        Assert.NotNull(med4);

        var delayResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/emar/{med4.Id}/delay", new DelayMedicationRequest
        {
            NewScheduledTimeUtc = DateTime.UtcNow.AddHours(6),
            Reason = "Hasta anjiyo ünitesine transfer edildi.",
        });
        Assert.Equal(HttpStatusCode.OK, delayResp.StatusCode);
        var delayed = await delayResp.Content.ReadFromJsonAsync<MedicationAdministrationResponse>();
        Assert.NotNull(delayed);
        Assert.Equal("Delayed", delayed.Status);

        // 8. Query all medications for admission
        var allMeds = await nurseClient.GetFromJsonAsync<List<MedicationAdministrationResponse>>($"/api/v1/inpatient/emar/admission/{admission.Id}");
        Assert.NotNull(allMeds);
        Assert.Equal(4, allMeds.Count);

        // 9. Query Due medications
        var dueMeds = await nurseClient.GetFromJsonAsync<List<MedicationAdministrationResponse>>($"/api/v1/inpatient/emar/due?admissionId={admission.Id}");
        Assert.NotNull(dueMeds);
        Assert.Single(dueMeds);
        Assert.Equal(med4.Id, dueMeds[0].Id);

        // 10. Concurrent administration records exactly one clinical outcome and returns 409 for the loser.
        var sched5Resp = await PostWithAntiforgeryAsync(
            nurseClient,
            "/api/v1/inpatient/emar/schedule",
            new ScheduleMedicationRequest
            {
                AdmissionId = admission.Id,
                PrescriptionId = signedPrescription.Id,
                MedicationName = prescribedItem.BrandName,
                Dose = $"{prescribedItem.Dose:0.####} {prescribedItem.DoseUnit}",
                Route = prescribedItem.Route,
                ScheduledTimeUtc = DateTime.UtcNow.AddHours(7),
            });
        Assert.Equal(HttpStatusCode.Created, sched5Resp.StatusCode);
        var med5 = await sched5Resp.Content.ReadFromJsonAsync<MedicationAdministrationResponse>();
        Assert.NotNull(med5);

        var secondNurseClient = CreateSecureClient(application);
        var secondNurseLogin = await LoginAsync(
            secondNurseClient,
            "DEMO-nurse@hospital.invalid",
            "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, secondNurseLogin.StatusCode);
        var competingAdministrations = await Task.WhenAll(
            PostWithAntiforgeryAsync(
                nurseClient,
                $"/api/v1/inpatient/emar/{med5.Id}/administer",
                admin1Req),
            PostWithAntiforgeryAsync(
                secondNurseClient,
                $"/api/v1/inpatient/emar/{med5.Id}/administer",
                admin1Req));
        Assert.Single(competingAdministrations, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(competingAdministrations, response => response.StatusCode == HttpStatusCode.Conflict);

        // 11. Negative Test: Anonymous request cannot schedule or administer
        var anonClient = application.CreateClient();
        var anonMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/inpatient/emar/schedule")
        {
            Content = JsonContent.Create(sched1Req),
        };
        var anonResp = await anonClient.SendAsync(anonMsg);
        Assert.True(anonResp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
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

        var inpatientDb = scope.ServiceProvider.GetRequiredService<InpatientDbContext>();
        await inpatientDb.Database.MigrateAsync();

        var orgSeeder = scope.ServiceProvider.GetRequiredService<IOrganizationDataSeeder>();
        await orgSeeder.SeedAsync();

        var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
        await identitySeeder.SeedAsync();

        var medicationSeeder = scope.ServiceProvider.GetRequiredService<IMedicationCatalogDataSeeder>();
        await medicationSeeder.SeedAsync();

        var inpatientSeeder = scope.ServiceProvider.GetRequiredService<IInpatientDataSeeder>();
        await inpatientSeeder.SeedAsync();
    }
}
