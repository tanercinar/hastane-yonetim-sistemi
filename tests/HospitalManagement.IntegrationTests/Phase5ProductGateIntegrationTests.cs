using System.Net;
using System.Net.Http.Json;

using HospitalManagement.Contracts.ClinicalRecords;
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

public sealed class Phase5ProductGateIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-KAPI")]
    public async Task Phase5FullPortfolioMvpGateEndToEndWorkflowAndIntegrityChecks()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAsync(application);
        await ClinicalTestData.SeedOrganizationAsync(application);

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();

            var pharmacySeeder = scope.ServiceProvider.GetRequiredService<IMedicationCatalogDataSeeder>();
            await pharmacySeeder.SeedAsync();
        }

        var receptionistClient = CreateSecureClient(application);
        var nurseClient = CreateSecureClient(application);
        var doctorClient = CreateSecureClient(application);
        var pharmacistClient = CreateSecureClient(application);
        var patientClient = CreateSecureClient(application);

        await LoginAsync(receptionistClient, "DEMO-receptionist@hospital.invalid", "DEMO-Recep-Pass!1");
        await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        await LoginAsync(pharmacistClient, "DEMO-pharmacist@hospital.invalid", "DEMO-Pharm-Pass!1");
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var departmentId = ClinicalTestData.DemoCardiologyDepartmentId;
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var nurseId = Guid.Parse("00000000-0000-0000-0000-000000000103");
        var appointmentId = Guid.NewGuid();

        // ---------------------------------------------------------------------------------
        // ADIM 1: Muayene (Encounter) Oluşturma ve Başlatma
        // ---------------------------------------------------------------------------------
        var createReq = new CreateEncounterRequest
        {
            AppointmentId = appointmentId,
            PatientId = patientId,
            DepartmentId = departmentId,
            PrimaryPractitionerId = doctorId,
            EncounterType = "Outpatient",
            PlannedStartTimeUtc = DateTime.UtcNow,
            ChiefComplaint = "Şiddetli boğaz ağrısı ve yüksek ateş",
        };

        var createResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-records/encounters", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var encounter = await createResp.Content.ReadFromJsonAsync<EncounterDetailResponse>();
        Assert.NotNull(encounter);
        var encounterId = encounter.Id;

        var startResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/encounters/{encounterId}/start",
            new StartEncounterRequest { ExpectedVersion = encounter.Version });
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startedEncounter = await startResp.Content.ReadFromJsonAsync<EncounterDetailResponse>();
        Assert.NotNull(startedEncounter);

        // Hemşire katılımcı olarak eklenir
        var participantResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/encounters/{encounterId}/participants",
            new AddEncounterParticipantRequest
            {
                ExpectedVersion = startedEncounter.Version,
                PractitionerId = nurseId,
                Role = "AssistingNurse",
            });
        Assert.Equal(HttpStatusCode.OK, participantResp.StatusCode);
        var encounterWithNurse = await participantResp.Content.ReadFromJsonAsync<EncounterDetailResponse>();
        Assert.NotNull(encounterWithNurse);

        // ---------------------------------------------------------------------------------
        // ADIM 2: Hemşire Vital Bulguları Kaydeder
        // ---------------------------------------------------------------------------------
        var panelReq = new RecordVitalSignsPanelRequest
        {
            PatientId = patientId,
            EncounterId = encounterId,
            TemperatureCelsius = 38.5m,
            SystolicBloodPressureMmHg = 120,
            DiastolicBloodPressureMmHg = 80,
            HeartRateBpm = 88,
            RespiratoryRatePerMin = 18,
            OxygenSaturationPercent = 98,
            BodyWeightKg = 74.0m,
            BodyHeightCm = 178,
            BloodGlucoseMgDl = 100,
            PainScore = 2,
            ConsciousnessState = "Alert",
            Notes = "Ön triyaj ve vital bulgular kaydedildi",
        };

        var panelResp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/clinical-records/vital-signs/panel", panelReq);
        Assert.Equal(HttpStatusCode.Created, panelResp.StatusCode);

        // ---------------------------------------------------------------------------------
        // ADIM 3: Doktor Tanı ve SOAP Notu Girer
        // ---------------------------------------------------------------------------------
        var diagReq = new CreateDiagnosisRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            DiagnosisType = "Final",
            IsCoded = true,
            Icd10Code = "J03.9",
            DiagnosisTitle = "Akut tonsillit, tanımlanmamış",
            Notes = "Klinik muayene ve tonsil eksüdasyonu ile uyumlu",
        };
        var diagResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-records/diagnoses", diagReq);
        Assert.Equal(HttpStatusCode.Created, diagResp.StatusCode);

        var noteReq = new CreateClinicalNoteRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            NoteType = "GeneralSoap",
            Title = "Poliklinik Muayene Notu",
            ChiefComplaint = "Boğaz ağrısı ve ateş",
            HistoryOfPresentIllness = "3 gündür devam eden yutkunma güçlüğü ve halsizlik.",
            PhysicalExamination = "Orofarenks hiperemik, bilateral tonsiller hipertrofik ve kriptik eksüdalı.",
            Assessment = "Akut bakteriyel tonsillit tablosu.",
            Plan = "Oral antibiyotik ve semptomatik tedavi başlandı.",
        };
        var noteResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-records/notes", noteReq);
        Assert.Equal(HttpStatusCode.Created, noteResp.StatusCode);
        var note = await noteResp.Content.ReadFromJsonAsync<ClinicalNoteResponse>();
        Assert.NotNull(note);

        var signNoteResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/notes/{note.Id}/sign",
            new SignClinicalNoteRequest
            {
                ExpectedVersion = note.Version,
                SignatureNote = "Hekim klinik onayı tamamlandı",
            });
        Assert.Equal(HttpStatusCode.OK, signNoteResp.StatusCode);

        // ---------------------------------------------------------------------------------
        // ADIM 4: Doktor İlaç Güvenlik Kontrolü ve Reçete Yazma (Safety Check & Sign)
        // ---------------------------------------------------------------------------------
        Guid amoxCatalogId;
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var pharmacyDb = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var amox = await pharmacyDb.MedicationCatalogItems.FirstAsync(m => m.Code == "DEMO-MED-AMX500");
            amoxCatalogId = amox.Id;
        }

        // Kural tabanlı güvenlik kontrolü
        var safetyCheckReq = new MedicationSafetyCheckRequest
        {
            PatientId = patientId,
            EncounterId = encounterId,
            Items =
            [
                new MedicationSafetyItemRequest
                {
                    MedicationCatalogItemId = amoxCatalogId,
                    Dose = 500,
                    DoseUnit = "mg",
                    Frequency = "2x1",
                    DurationDays = 7,
                },
            ],
        };
        var safetyResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/pharmacy/prescriptions/safety-check", safetyCheckReq);
        Assert.Equal(HttpStatusCode.OK, safetyResp.StatusCode);
        var safetyCheckResult = await safetyResp.Content.ReadFromJsonAsync<MedicationSafetyCheckResponse>();
        Assert.NotNull(safetyCheckResult);
        Assert.NotNull(safetyCheckResult.Disclaimer);

        // Reçete Taslağı Oluşturma
        var createRxReq = new CreatePrescriptionDraftRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            DepartmentId = departmentId,
            DiagnosisSummary = "Akut Tonsillit",
            GeneralInstructions = "Yemeklerden sonra bol su ile alınız.",
            Items =
            [
                new CreatePrescriptionDraftItemRequest
                {
                    MedicationCatalogItemId = amoxCatalogId,
                    Dose = 500,
                    DoseUnit = "mg",
                    Frequency = "2x1",
                    DurationDays = 7,
                    Quantity = 2,
                    QuantityUnit = "kutu",
                    Instructions = "12 saatte bir 1 kapsül",
                },
            ],
        };

        var rxCreateResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/pharmacy/prescriptions", createRxReq);
        Assert.Equal(HttpStatusCode.Created, rxCreateResp.StatusCode);
        var rxDraft = await rxCreateResp.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(rxDraft);

        // Doktor Reçeteyi İmzalar (İmzalı reçete klinik kilitli ve değişmezdir)
        var signRxResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/pharmacy/prescriptions/{rxDraft.Id}/sign",
            new SignPrescriptionRequest
            {
                ExpectedVersion = rxDraft.Version,
                ValidDays = 14,
                OverrideReason = "Klinik değerlendirme neticesinde tedavi kararlaştırıldı",
            });
        Assert.Equal(HttpStatusCode.OK, signRxResp.StatusCode);
        var signedRx = await signRxResp.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(signedRx);
        Assert.Equal("Signed", signedRx.Status);

        // Muayeneyi Tamamla
        var completeEncounterResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/encounters/{encounterId}/complete",
            new CompleteEncounterRequest
            {
                ExpectedVersion = encounterWithNurse.Version,
                Summary = "Muayene ve reçete başarıyla tamamlandı.",
            });
        Assert.Equal(HttpStatusCode.OK, completeEncounterResp.StatusCode);

        // ---------------------------------------------------------------------------------
        // ADIM 5: Eczacı İş Listesi, Klinik İzolasyon ve FEFO Teslimi (Dispense)
        // ---------------------------------------------------------------------------------
        // 5.1 Eczacı iş listesinde reçeteyi bulur
        var worklistResp = await pharmacistClient.GetAsync($"/api/v1/pharmacy/prescriptions/worklist?prescriptionNumber={signedRx.PrescriptionNumber}");
        Assert.Equal(HttpStatusCode.OK, worklistResp.StatusCode);
        var worklist = await worklistResp.Content.ReadFromJsonAsync<List<PrescriptionSummaryResponse>>();
        Assert.NotNull(worklist);
        Assert.Contains(worklist, w => w.Id == signedRx.Id);

        // 5.2 Klinik İzolasyon Testi: Eczacı doğrudan hekimin SOAP notlarına erişemez
        var unauthorizedClinicalResp = await pharmacistClient.GetAsync($"/api/v1/clinical-records/notes/by-encounter/{encounterId}");
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedClinicalResp.StatusCode);

        // 5.3 FEFO Miat Sıralamalı Stok Partisi Seçimi
        var fefoResp = await pharmacistClient.GetAsync($"/api/v1/pharmacy/inventory/fefo-candidates/{amoxCatalogId}");
        Assert.Equal(HttpStatusCode.OK, fefoResp.StatusCode);
        var fefoLots = await fefoResp.Content.ReadFromJsonAsync<List<FefoCandidateStockResponse>>();
        Assert.NotNull(fefoLots);
        Assert.True(fefoLots.Count >= 2);
        // FEFO kuralı: İlk parti, ikinci partiden daha erken miatlıdır
        Assert.True(fefoLots[0].ExpirationDateUtc <= fefoLots[1].ExpirationDateUtc);
        var preferredLot = fefoLots[0];

        // 5.4 Eczacı Tam İlaç Teslimini Gerçekleştirir (2 kutu)
        var dispenseReq = new DispensePrescriptionRequest
        {
            IdempotencyKey = Guid.NewGuid(),
            ExpectedVersion = signedRx.Version,
            Items =
            [
                new DispensePrescriptionItemRequest
                {
                    ItemId = signedRx.Items[0].Id,
                    StockItemId = preferredLot.StockItemId,
                    ExpectedStockVersion = preferredLot.Version,
                    Quantity = 2,
                    Notes = "Eczaneden tam teslim edildi",
                },
            ],
        };

        var dispenseResp = await PostWithAntiforgeryAsync(
            pharmacistClient,
            $"/api/v1/pharmacy/prescriptions/{signedRx.Id}/dispense",
            dispenseReq);
        Assert.Equal(HttpStatusCode.OK, dispenseResp.StatusCode);
        var dispensedRx = await dispenseResp.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(dispensedRx);
        Assert.Equal("Dispensed", dispensedRx.Status);
        Assert.Equal(2, dispensedRx.Items[0].DispensedQuantity);
        Assert.True(dispensedRx.Items[0].IsFullyDispensed);

        // ---------------------------------------------------------------------------------
        // ADIM 6: Hasta Portalı Doğrulaması (Hasta Reçetelerim Ekranı)
        // ---------------------------------------------------------------------------------
        var patientRxListResp = await patientClient.GetAsync($"/api/v1/pharmacy/prescriptions/by-patient/{patientId}");
        Assert.Equal(HttpStatusCode.OK, patientRxListResp.StatusCode);
        var patientRxList = await patientRxListResp.Content.ReadFromJsonAsync<List<PrescriptionSummaryResponse>>();
        Assert.NotNull(patientRxList);
        Assert.Contains(patientRxList, p => p.Id == signedRx.Id && p.Status == "Dispensed");

        var patientDetailResp = await patientClient.GetAsync($"/api/v1/pharmacy/prescriptions/{signedRx.Id}");
        Assert.Equal(HttpStatusCode.OK, patientDetailResp.StatusCode);
        var patientRxDetail = await patientDetailResp.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(patientRxDetail);
        Assert.Equal("Akut Tonsillit", patientRxDetail.DiagnosisSummary);

        // ---------------------------------------------------------------------------------
        // ADIM 7: Güvenlik, IDOR, Stok Bütünlüğü ve Denetim İzi Doğrulamaları
        // ---------------------------------------------------------------------------------
        // 7.1 Hasta IDOR Koruması: Başka hastanın kimliği sorgulanamaz
        var idorResp = await patientClient.GetAsync($"/api/v1/pharmacy/prescriptions/by-patient/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, idorResp.StatusCode);

        // 7.2 Stok Miktar ve Hareket Bütünlüğü
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var pharmacyDb = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var stockItem = await pharmacyDb.MedicationStockItems.FirstAsync(s => s.Id == preferredLot.StockItemId);
            Assert.Equal(48, stockItem.QuantityOnHand); // Başlangıç 50 - 2 = 48

            var transactions = await pharmacyDb.MedicationStockTransactions
                .Where(t => t.StockItemId == preferredLot.StockItemId && t.TransactionType == StockTransactionType.Dispense)
                .ToListAsync();
            Assert.Single(transactions);
            Assert.Equal(2, transactions[0].Quantity);
            Assert.Equal(signedRx.PrescriptionNumber, transactions[0].ReferenceId);
        }

        // 7.3 Uçtan Uca Denetim İzi (Audit Log)
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var logs = await auditDb.AuditLogs.AsNoTracking().ToListAsync();

            Assert.Contains(logs, l => l.Action == "Pharmacy.PrescriptionCreateDraft");
            Assert.Contains(logs, l => l.Action == "Pharmacy.PrescriptionSign");
            Assert.Contains(logs, l => l.Action == "Pharmacy.PrescriptionDispense");
            Assert.Contains(logs, l => l.Action == "ClinicalRecords.EncounterComplete");
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

    private static async Task RunAllMigrationsAsync(ApiWebApplicationFactory application)
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
    }
}
