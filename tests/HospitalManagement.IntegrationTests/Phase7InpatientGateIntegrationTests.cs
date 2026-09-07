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
using HospitalManagement.Modules.Inpatient.Domain;
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

public sealed class Phase7InpatientGateIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F07-KAPI")]
    public async Task Phase7GateFullInpatientLifecycleAdmissionToTransferToNursingToEmarToDischargeSucceeds()
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
        var orthDeptId = Guid.Parse("30000000-0000-0000-0000-000000000002");

        // Step 1: Doctor requests admission
        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(application, patientId);
        var prescriptionDraftResponse = await PostWithAntiforgeryAsync(
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
            prescriptionDraftResponse.StatusCode == HttpStatusCode.Created,
            await prescriptionDraftResponse.Content.ReadAsStringAsync());
        var prescriptionDraft = await prescriptionDraftResponse.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(prescriptionDraft);
        var prescriptionSignResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/pharmacy/prescriptions/{prescriptionDraft.Id}/sign",
            new SignPrescriptionRequest { ExpectedVersion = prescriptionDraft.Version, ValidDays = 7 });
        Assert.Equal(HttpStatusCode.OK, prescriptionSignResponse.StatusCode);
        var signedPrescription = await prescriptionSignResponse.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(signedPrescription);
        var prescribedItem = Assert.Single(signedPrescription.Items);

        var wards = await doctorClient.GetFromJsonAsync<List<WardResponse>>("/api/v1/inpatient/wards");
        Assert.NotNull(wards);
        var cardWard = wards.Single(w => w.Code == "DEMO-WRD-CARD");
        Assert.DoesNotContain(wards, ward => ward.Code == "DEMO-WRD-INTMED");

        var outOfDepartmentWardResponse = await doctorClient.GetAsync(
            "/api/v1/inpatient/wards/50000000-0000-0000-0000-000000000002");
        Assert.Equal(HttpStatusCode.Forbidden, outOfDepartmentWardResponse.StatusCode);

        var createAdmissionReq = new CreateAdmissionRequest
        {
            PatientId = patientId,
            EncounterId = encounterId,
            DepartmentId = cardDeptId,
            AdmittingWardId = cardWard.Id,
            AttendingDoctorId = doctorPersonId,
            AdmissionReason = "Akut Koroner Sendrom ve Post-Anjiyo İzlem",
            DietType = "LowSodium",
            FallRiskScore = 45,
            IsolationRequired = "None",
            EstimatedStayDays = 4,
        };

        var admResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/admissions", createAdmissionReq);
        Assert.Equal(HttpStatusCode.Created, admResp.StatusCode);
        var admission = await admResp.Content.ReadFromJsonAsync<AdmissionResponse>();
        Assert.NotNull(admission);
        Assert.Equal("Requested", admission.Status);

        // Step 2: Nurse accepts and admits patient to Bed A in Cardiology
        var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        var outOfDepartmentBedsResponse = await nurseClient.GetAsync(
            "/api/v1/inpatient/beds?wardId=50000000-0000-0000-0000-000000000002");
        Assert.Equal(HttpStatusCode.Forbidden, outOfDepartmentBedsResponse.StatusCode);

        var acceptResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{admission.Id}/accept", new AcceptAdmissionRequest());
        Assert.Equal(HttpStatusCode.OK, acceptResp.StatusCode);

        var cardBeds = await nurseClient.GetFromJsonAsync<List<BedResponse>>($"/api/v1/inpatient/beds?wardId={cardWard.Id}&status=Available");
        Assert.NotNull(cardBeds);
        var bedA = cardBeds.First();

        var admitResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{admission.Id}/admit", new AdmitPatientRequest { BedId = bedA.Id });
        Assert.Equal(HttpStatusCode.OK, admitResp.StatusCode);

        // Step 3: Verify Bed A is Occupied & Clinical Board displays patient
        var bedAResult = await nurseClient.GetFromJsonAsync<BedResponse>($"/api/v1/inpatient/beds/{bedA.Id}");
        Assert.NotNull(bedAResult);
        Assert.Equal("Occupied", bedAResult.Status);

        var boardItems = await nurseClient.GetFromJsonAsync<List<InpatientBoardItemResponse>>($"/api/v1/inpatient/board?wardId={cardWard.Id}");
        Assert.NotNull(boardItems);
        Assert.Contains(boardItems, b => b.AdmissionId == admission.Id);

        // Step 4: Nurse records periodic observation
        var obsReq = new RecordObservationRequest
        {
            AdmissionId = admission.Id,
            SystolicBp = 120,
            DiastolicBp = 80,
            HeartRate = 72,
            BodyTemperatureCelsius = 36.5m,
            RespiratoryRate = 16,
            OxygenSaturationPercent = 98,
            PainScale = 2,
            Consciousness = "Alert",
            OralIntakeMl = 300,
            UrineOutputMl = 250,
            ClinicalNotes = "Hasta stabil, vital bulgular olağan.",
        };
        var obsResp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/inpatient/nursing/observations", obsReq);
        Assert.Equal(HttpStatusCode.Created, obsResp.StatusCode);

        // Step 5: Nurse creates Care Plan and Care Task
        var carePlanReq = new CreateCarePlanRequest
        {
            AdmissionId = admission.Id,
            NursingDiagnosis = "Akut Ağrı ve Dolaşım İzlemi",
            Goal = "Ağrı skorunun <= 2 tutulması ve hemodinaminin korunması",
        };
        var carePlanResp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/inpatient/nursing/care-plans", carePlanReq);
        Assert.Equal(HttpStatusCode.Created, carePlanResp.StatusCode);
        var carePlan = await carePlanResp.Content.ReadFromJsonAsync<NursingCarePlanResponse>();
        Assert.NotNull(carePlan);

        var taskReq = new AddCareTaskRequest
        {
            Title = "2 saatte bir vital takibi",
            Frequency = "q2h",
            DueTimeUtc = DateTime.UtcNow.AddHours(2),
        };
        var taskResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/nursing/care-plans/{carePlan.Id}/tasks", taskReq);
        Assert.Equal(HttpStatusCode.Created, taskResp.StatusCode);
        var careTask = await taskResp.Content.ReadFromJsonAsync<NursingCareTaskResponse>();
        Assert.NotNull(careTask);

        var completeTaskResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/nursing/tasks/{careTask.Id}/complete", new CompleteCareTaskRequest { Notes = "Vital takibi yapıldı, değerler normal." });
        Assert.Equal(HttpStatusCode.OK, completeTaskResp.StatusCode);

        // Step 6: eMAR - Schedule and Administer dose with 5 Rights
        var scheduleDoseReq = new ScheduleMedicationRequest
        {
            AdmissionId = admission.Id,
            PrescriptionId = signedPrescription.Id,
            MedicationName = prescribedItem.BrandName,
            Dose = $"{prescribedItem.Dose:0.####} {prescribedItem.DoseUnit}",
            Route = prescribedItem.Route,
            ScheduledTimeUtc = DateTime.UtcNow.AddMinutes(15),
        };
        var scheduleResp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/inpatient/emar/schedule", scheduleDoseReq);
        Assert.Equal(HttpStatusCode.Created, scheduleResp.StatusCode);
        var dose = await scheduleResp.Content.ReadFromJsonAsync<MedicationAdministrationResponse>();
        Assert.NotNull(dose);

        var administerReq = new AdministerMedicationRequest
        {
            Verified5Rights = true,
            Notes = "Doğru hasta, ilaç, doz, zaman, yol doğrulandı. Oral verildi.",
        };
        var adminDoseResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/emar/{dose.Id}/administer", administerReq);
        Assert.Equal(HttpStatusCode.OK, adminDoseResp.StatusCode);

        // Step 7: Nurse requests an authorized in-ward room/bed transfer.
        var transferBeds = await nurseClient.GetFromJsonAsync<List<BedResponse>>($"/api/v1/inpatient/beds?wardId={cardWard.Id}&status=Available");
        Assert.NotNull(transferBeds);
        var bedB = transferBeds.First(bed => bed.Id != bedA.Id);

        var transferReq = new CreateTransferRequest
        {
            AdmissionId = admission.Id,
            TargetWardId = cardWard.Id,
            TargetBedId = bedB.Id,
            TransferReason = "İzlem gereksinimi nedeniyle oda ve yatak değişikliği.",
            ClinicalNotes = "DEMO klinik transfer notu.",
        };
        var transferResp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/inpatient/transfers", transferReq);
        Assert.Equal(HttpStatusCode.Created, transferResp.StatusCode);
        var transfer = await transferResp.Content.ReadFromJsonAsync<TransferResponse>();
        Assert.NotNull(transfer);

        // Step 8: Nurse accepts and completes transfer
        var acceptTransferResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/inpatient/transfers/{transfer.Id}/accept",
            new AcceptTransferRequest { TargetBedId = bedB.Id });
        Assert.Equal(HttpStatusCode.OK, acceptTransferResponse.StatusCode);
        var completeTransferResp = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/inpatient/transfers/{transfer.Id}/complete",
            new CompleteTransferRequest { TargetBedId = bedB.Id });
        Assert.Equal(HttpStatusCode.OK, completeTransferResp.StatusCode);

        // Step 9: Verify Bed A transitioned to Cleaning and Bed B is Occupied
        var bedACleaning = await nurseClient.GetFromJsonAsync<BedResponse>($"/api/v1/inpatient/beds/{bedA.Id}");
        Assert.NotNull(bedACleaning);
        Assert.Equal("Cleaning", bedACleaning.Status);

        var bedBOccupied = await nurseClient.GetFromJsonAsync<BedResponse>($"/api/v1/inpatient/beds/{bedB.Id}");
        Assert.NotNull(bedBOccupied);
        Assert.Equal("Occupied", bedBOccupied.Status);

        // Step 10: Doctor completes discharge
        var dischargeReq = new DischargeAdmissionRequest
        {
            AdmissionId = admission.Id,
            DischargeType = "Home",
            DischargeSummary = "Hasta tüm yatış sürecinde takip edilmiş olup klinik şifa ile taburculuğu uygun bulunmuştur.",
            FinalDiagnosisCode = "I25.1",
            FinalDiagnosisDescription = "Aterosklerotik Kalp Hastalığı",
            DischargeRecommendations = "Düşük sodyumlu beslenme ve 1 hafta sonra poliklinik kontrolü.",
            DischargePrescriptionSummary = "DEMO-Aspirin 100mg 1x1, DEMO-Metoprolol 25mg 1x1",
            FollowUpAppointmentDateUtc = DateTime.UtcNow.AddDays(7),
        };
        var dischargeResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/discharges", dischargeReq);
        Assert.Equal(HttpStatusCode.Created, dischargeResp.StatusCode);
        var discharge = await dischargeResp.Content.ReadFromJsonAsync<InpatientDischargeResponse>();
        Assert.NotNull(discharge);

        // Step 11: Verify Bed B is Cleaning, Admission is Discharged, Dashboard reflects 1 discharge
        var bedBCleaning = await nurseClient.GetFromJsonAsync<BedResponse>($"/api/v1/inpatient/beds/{bedB.Id}");
        Assert.NotNull(bedBCleaning);
        Assert.Equal("Cleaning", bedBCleaning.Status);

        var closedAdmission = await doctorClient.GetFromJsonAsync<AdmissionResponse>($"/api/v1/inpatient/admissions/{admission.Id}");
        Assert.NotNull(closedAdmission);
        Assert.Equal("Discharged", closedAdmission.Status);

        var dashboard = await doctorClient.GetFromJsonAsync<InpatientDashboardResponse>("/api/v1/inpatient/dashboard");
        Assert.NotNull(dashboard);
        Assert.Equal(1, dashboard.TodayDischargesCount);

        await using var auditScope = application.Services.CreateAsyncScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var inpatientAuditDetails = await auditDb.AuditLogs
            .AsNoTracking()
            .Where(entry => entry.Action.StartsWith("Inpatient."))
            .Select(entry => entry.DetailsJson ?? string.Empty)
            .ToListAsync();
        var serializedAuditDetails = string.Join('\n', inpatientAuditDetails);
        Assert.DoesNotContain("Akut Koroner Sendrom", serializedAuditDetails, StringComparison.Ordinal);
        Assert.DoesNotContain("Hasta stabil", serializedAuditDetails, StringComparison.Ordinal);
        Assert.DoesNotContain("Akut Ağrı", serializedAuditDetails, StringComparison.Ordinal);
        Assert.DoesNotContain(prescribedItem.BrandName, serializedAuditDetails, StringComparison.Ordinal);
        Assert.DoesNotContain("Aterosklerotik Kalp Hastalığı", serializedAuditDetails, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F07-KAPI")]
    public async Task Phase7GateNegativeInvariantsAndConcurrencyEnforcesIntegrity()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patient1Id = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var patient2Id = Guid.Parse("00000000-0000-0000-0000-000000000110");
        var doctorPersonId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var cardDeptId = Guid.Parse("30000000-0000-0000-0000-000000000003");

        var doctorClient = CreateSecureClient(application);
        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        var secondDoctorClient = CreateSecureClient(application);
        await LoginAsync(secondDoctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");

        var nurseClient = CreateSecureClient(application);
        await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        var secondNurseClient = CreateSecureClient(application);
        await LoginAsync(secondNurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");

        var wards = await doctorClient.GetFromJsonAsync<List<WardResponse>>("/api/v1/inpatient/wards");
        Assert.NotNull(wards);
        var cardWard = wards.Single(w => w.Code == "DEMO-WRD-CARD");

        // Invariant 1: Duplicate active admission request for the same patient returns 409 Conflict
        var admReq1 = new CreateAdmissionRequest
        {
            PatientId = patient1Id,
            DepartmentId = cardDeptId,
            AdmittingWardId = cardWard.Id,
            AttendingDoctorId = doctorPersonId,
            AdmissionReason = "İlk Aktif Yatış Talebi",
            DietType = "Standard",
            FallRiskScore = 10,
            IsolationRequired = "None",
        };
        var competingAdmissionRequests = await Task.WhenAll(
            PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/admissions", admReq1),
            PostWithAntiforgeryAsync(secondDoctorClient, "/api/v1/inpatient/admissions", admReq1));
        var resp1 = Assert.Single(
            competingAdmissionRequests,
            response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(
            competingAdmissionRequests,
            response => response.StatusCode == HttpStatusCode.Conflict);
        var adm1 = await resp1.Content.ReadFromJsonAsync<AdmissionResponse>();
        Assert.NotNull(adm1);

        // Invariant 2: Double Bed Assignment - Cannot admit 2 patients to the same bed
        var admReq2 = new CreateAdmissionRequest
        {
            PatientId = patient2Id,
            DepartmentId = cardDeptId,
            AdmittingWardId = cardWard.Id,
            AttendingDoctorId = doctorPersonId,
            AdmissionReason = "İkinci Hasta Yatış Talebi",
            DietType = "Standard",
            FallRiskScore = 10,
            IsolationRequired = "None",
        };
        var resp2 = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/admissions", admReq2);
        Assert.Equal(HttpStatusCode.Created, resp2.StatusCode);
        var adm2 = await resp2.Content.ReadFromJsonAsync<AdmissionResponse>();
        Assert.NotNull(adm2);

        await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{adm1.Id}/accept", new AcceptAdmissionRequest());
        await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{adm2.Id}/accept", new AcceptAdmissionRequest());

        var beds = await nurseClient.GetFromJsonAsync<List<BedResponse>>($"/api/v1/inpatient/beds?wardId={cardWard.Id}&status=Available");
        Assert.NotNull(beds);
        var sharedBed = beds.First();

        var competingAdmissions = await Task.WhenAll(
            PostWithAntiforgeryAsync(
                nurseClient,
                $"/api/v1/inpatient/admissions/{adm1.Id}/admit",
                new AdmitPatientRequest { BedId = sharedBed.Id }),
            PostWithAntiforgeryAsync(
                secondNurseClient,
                $"/api/v1/inpatient/admissions/{adm2.Id}/admit",
                new AdmitPatientRequest { BedId = sharedBed.Id }));
        Assert.Single(competingAdmissions, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(competingAdmissions, response => response.StatusCode == HttpStatusCode.Conflict);
        var admittedAdmissionId = competingAdmissions[0].StatusCode == HttpStatusCode.OK ? adm1.Id : adm2.Id;

        // Invariant 3: Short Discharge Summary (< 20 chars) returns Validation error
        var shortDischargeReq = new DischargeAdmissionRequest
        {
            AdmissionId = admittedAdmissionId,
            DischargeType = "Home",
            DischargeSummary = "Kısa özet",
            FinalDiagnosisCode = "I25.1",
            FinalDiagnosisDescription = "Tanı",
        };
        var shortSummaryResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/discharges", shortDischargeReq);
        Assert.Equal(HttpStatusCode.BadRequest, shortSummaryResp.StatusCode);

        // Invariant 4: Admin cannot perform clinical actions (Discharge or Administer medication)
        var adminClient = CreateSecureClient(application);
        await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1");

        var validDischargeReq = new DischargeAdmissionRequest
        {
            AdmissionId = admittedAdmissionId,
            DischargeType = "Home",
            DischargeSummary = "Hasta taburculuk kriterlerini eksiksiz karşılamış olup taburcu edilmiştir.",
            FinalDiagnosisCode = "I25.1",
            FinalDiagnosisDescription = "Aterosklerotik Kalp Hastalığı",
        };
        var adminDischargeResp = await PostWithAntiforgeryAsync(adminClient, "/api/v1/inpatient/discharges", validDischargeReq);
        Assert.Equal(HttpStatusCode.Forbidden, adminDischargeResp.StatusCode);

        // Invariant 5: Concurrent discharge requests create exactly one discharge and return 409 for the loser.
        var competingDischarges = await Task.WhenAll(
            PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/discharges", validDischargeReq),
            PostWithAntiforgeryAsync(secondDoctorClient, "/api/v1/inpatient/discharges", validDischargeReq));
        Assert.Single(competingDischarges, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(competingDischarges, response => response.StatusCode == HttpStatusCode.Conflict);

        await using var verificationScope = application.Services.CreateAsyncScope();
        var inpatientDb = verificationScope.ServiceProvider.GetRequiredService<InpatientDbContext>();
        Assert.Equal(
            1,
            await inpatientDb.Discharges.CountAsync(discharge =>
                discharge.AdmissionId == admittedAdmissionId));
        var dischargedAdmission = await inpatientDb.Admissions
            .AsNoTracking()
            .SingleAsync(admission => admission.Id == admittedAdmissionId);
        Assert.Equal(AdmissionStatus.Discharged, dischargedAdmission.Status);
        var releasedBed = await inpatientDb.Beds
            .AsNoTracking()
            .SingleAsync(bed => bed.Id == sharedBed.Id);
        Assert.Equal(BedStatus.Cleaning, releasedBed.Status);
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
