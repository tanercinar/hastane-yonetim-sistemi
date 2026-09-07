using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using HospitalManagement.Contracts.ClinicalRecords;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class ClinicalPhase4GateTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-KAPI")]
    public async Task Phase4ClinicalRecordsGateMultidisciplinaryWorkflowAndIntegrityChecks()
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
        }

        var doctorClient = CreateSecureClient(application);
        var nurseClient = CreateSecureClient(application);
        var patientClient = CreateSecureClient(application);

        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var departmentId = ClinicalTestData.DemoCardiologyDepartmentId;
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var nurseId = Guid.Parse("00000000-0000-0000-0000-000000000103");
        var appointmentId = Guid.NewGuid();

        // -------------------------------------------------------------
        // Step 1: Doctor creates encounter
        // -------------------------------------------------------------
        var createReq = new CreateEncounterRequest
        {
            AppointmentId = appointmentId,
            PatientId = patientId,
            DepartmentId = departmentId,
            PrimaryPractitionerId = doctorId,
            EncounterType = "Outpatient",
            PlannedStartTimeUtc = DateTime.UtcNow,
            ChiefComplaint = "Göğüs sıkışması, çarpıntı ve baş dönmesi",
        };

        var createResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-records/encounters", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var encounter = await createResp.Content.ReadFromJsonAsync<EncounterDetailResponse>();
        Assert.NotNull(encounter);
        var encounterId = encounter.Id;

        // -------------------------------------------------------------
        // Step 2: Doctor starts encounter and Nurse records vital signs panel
        // -------------------------------------------------------------
        var startResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/encounters/{encounterId}/start",
            new StartEncounterRequest { ExpectedVersion = encounter.Version });
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startedEncounter = await startResp.Content.ReadFromJsonAsync<EncounterDetailResponse>();
        Assert.NotNull(startedEncounter);

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

        var panelReq = new RecordVitalSignsPanelRequest
        {
            PatientId = patientId,
            EncounterId = encounterId,
            TemperatureCelsius = 36.8m,
            SystolicBloodPressureMmHg = 145,
            DiastolicBloodPressureMmHg = 95,
            HeartRateBpm = 88,
            RespiratoryRatePerMin = 18,
            OxygenSaturationPercent = 98,
            BodyWeightKg = 82.5m,
            BodyHeightCm = 178,
            BloodGlucoseMgDl = 110,
            PainScore = 3,
            ConsciousnessState = "Alert",
            Notes = "Hemşire ön triyaj değerlendirmesi tamamlandı.",
        };
        var panelResp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/clinical-records/vital-signs/panel", panelReq);
        Assert.Equal(HttpStatusCode.Created, panelResp.StatusCode);
        var panelResult = await panelResp.Content.ReadFromJsonAsync<VitalSignsPanelResponse>();
        Assert.NotNull(panelResult);
        Assert.True(panelResult.Observations.Count >= 7);

        // -------------------------------------------------------------
        // Step 3: Doctor records Allergy and Problem
        // -------------------------------------------------------------
        var allergyReq = new CreateAllergyRequest
        {
            PatientId = patientId,
            EncounterId = encounterId,
            Substance = "Penisilin",
            Category = "Medication",
            Criticality = "High",
            Manifestation = "Deri döküntüsü ve anjiyoödem",
            OnsetDateTimeUtc = DateTime.UtcNow.AddYears(-2),
            Notes = "Daha önce ampisilin sonrası gelişmiş.",
        };
        var allergyResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-records/allergies", allergyReq);
        Assert.Equal(HttpStatusCode.Created, allergyResp.StatusCode);

        var problemReq = new CreateClinicalProblemRequest
        {
            PatientId = patientId,
            EncounterId = encounterId,
            ProblemTitle = "Primer Hipertansiyon",
            Code = "I10",
            Category = "ChronicCondition",
            OnsetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            Notes = "Antihipertansif tedavi planlanıyor.",
        };
        var problemResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-records/problems", problemReq);
        Assert.Equal(HttpStatusCode.Created, problemResp.StatusCode);

        // -------------------------------------------------------------
        // Step 4: Doctor searches diagnosis catalog & attaches coded diagnosis
        // -------------------------------------------------------------
        var catalogSearchResp = await doctorClient.GetAsync("/api/v1/clinical-records/diagnosis-catalog?query=hipertansiyon");
        Assert.Equal(HttpStatusCode.OK, catalogSearchResp.StatusCode);
        var catalogItems = await catalogSearchResp.Content.ReadFromJsonAsync<List<DiagnosisCatalogItemResponse>>();
        Assert.NotNull(catalogItems);
        Assert.NotEmpty(catalogItems);

        var diagReq = new CreateDiagnosisRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            DiagnosisType = "Final",
            IsCoded = true,
            Icd10Code = "I10",
            DiagnosisTitle = "Esansiyel hipertansiyon",
            Notes = "Evre 1 hipertansiyon tanısı konuldu.",
        };
        var diagResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-records/diagnoses", diagReq);
        Assert.Equal(HttpStatusCode.Created, diagResp.StatusCode);

        // -------------------------------------------------------------
        // Step 5: Doctor creates SOAP note and signs it
        // -------------------------------------------------------------
        var noteReq = new CreateClinicalNoteRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            NoteType = "GeneralSoap",
            Title = "Poliklinik Kardiyoloji Muayenesi",
            ChiefComplaint = "Göğüs sıkışması ve çarpıntı",
            HistoryOfPresentIllness = "Son 2 haftadır eforla artan göğüs basısı tarifliyor.",
            PhysicalExamination = "Kalp sesleri ritmik, ek ses ve üfürüm yok. Solunum sesleri doğal.",
            Assessment = "Evre 1 Esansiyel Hipertansiyon. Kardiyak iskemi ekarte edilmek üzere EKG ve EKO planlandı.",
            Plan = "Tansiyon holteri takılacak, tuzsuz diyet önerildi, ACE inhibitörü başlandı.",
        };
        var noteResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-records/notes", noteReq);
        Assert.Equal(HttpStatusCode.Created, noteResp.StatusCode);
        var note = await noteResp.Content.ReadFromJsonAsync<ClinicalNoteResponse>();
        Assert.NotNull(note);

        var signResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/notes/{note.Id}/sign",
            new SignClinicalNoteRequest
            {
                ExpectedVersion = note.Version,
                SignatureNote = "Dijital hekim imzası onaylandı",
            });
        Assert.Equal(HttpStatusCode.OK, signResp.StatusCode);
        var signedNote = await signResp.Content.ReadFromJsonAsync<ClinicalNoteResponse>();
        Assert.NotNull(signedNote);

        // -------------------------------------------------------------
        // Step 6: Security & Immutability Test -> Cannot modify signed note
        // -------------------------------------------------------------
        var modifySignedNoteReq = new UpdateClinicalNoteDraftRequest
        {
            ExpectedVersion = signedNote.Version,
            Title = "Değiştirilmiş Başlık",
            Content = "Sessizce değiştirilmeye çalışılan içerik",
        };
        var token = await doctorClient.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/v1/identity/antiforgery");
        Assert.NotNull(token);

        using var putReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/clinical-records/notes/{note.Id}")
        {
            Content = JsonContent.Create(modifySignedNoteReq),
        };
        putReq.Headers.Add("X-HMS-CSRF", token.Token);
        var modifyResp = await doctorClient.SendAsync(putReq);
        Assert.Equal(HttpStatusCode.Conflict, modifyResp.StatusCode); // 409 Conflict: Signed note cannot be modified!

        // -------------------------------------------------------------
        // Step 7: Doctor uploads clinical attachment PDF
        // -------------------------------------------------------------
        var pdfBytes = "%PDF-1.4\n1 0 obj\n<< /Title (EKG Raporu) >>\nendobj\n%%EOF"u8.ToArray();
        using var uploadForm = new MultipartFormDataContent();
        uploadForm.Add(new StringContent(encounterId.ToString()), "encounterId");
        uploadForm.Add(new StringContent(patientId.ToString()), "patientId");
        uploadForm.Add(new StringContent("RadiologyImage"), "attachmentType");
        uploadForm.Add(new StringContent("12 Derivasyonlu İstirahat EKG'si"), "description");

        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        uploadForm.Add(fileContent, "file", "ekg_report.pdf");

        using var uploadReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/clinical-records/attachments")
        {
            Content = uploadForm,
        };
        uploadReq.Headers.Add("X-HMS-CSRF", token.Token);
        var uploadResp = await doctorClient.SendAsync(uploadReq);
        Assert.Equal(HttpStatusCode.Created, uploadResp.StatusCode);

        // -------------------------------------------------------------
        // Step 8: Doctor requests Cardiology Consultation
        // -------------------------------------------------------------
        var consultReq = new CreateConsultationRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            TargetDepartmentId = departmentId,
            Urgency = "Urgent",
            ReasonForConsultation = "Efor EKG değerlendirmesi",
            ClinicalQuestion = "İskemi bulgusu açısından eko ve efor testi gerekliliği konsülte edilmektedir.",
        };
        var consultResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-records/consultations", consultReq);
        Assert.Equal(HttpStatusCode.Created, consultResp.StatusCode);

        // -------------------------------------------------------------
        // Step 9: Complete encounter successfully
        // -------------------------------------------------------------
        var completeResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/encounters/{encounterId}/complete",
            new CompleteEncounterRequest
            {
                ExpectedVersion = encounterWithNurse.Version,
                Summary = "Poliklinik muayenesi eksiksiz tamamlandı.",
            });
        Assert.Equal(HttpStatusCode.OK, completeResp.StatusCode);
        var completedEnc = await completeResp.Content.ReadFromJsonAsync<EncounterDetailResponse>();
        Assert.NotNull(completedEnc);
        Assert.Equal("Completed", completedEnc.Status);

        // -------------------------------------------------------------
        // Step 10: Verify Patient Longitudinal Timeline aggregation
        // -------------------------------------------------------------
        var timelineResp = await patientClient.GetAsync($"/api/v1/clinical-records/timeline/by-patient/{patientId}?pageNumber=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, timelineResp.StatusCode);
        var timeline = await timelineResp.Content.ReadFromJsonAsync<PatientTimelinePagedResponse>();
        Assert.NotNull(timeline);

        Assert.Contains(timeline.Items, i => i.EventType == "Encounter");
        Assert.Contains(timeline.Items, i => i.EventType == "VitalSigns");
        Assert.Contains(timeline.Items, i => i.EventType == "Allergy");
        Assert.Contains(timeline.Items, i => i.EventType == "Problem");
        Assert.Contains(timeline.Items, i => i.EventType == "Diagnosis");
        Assert.Contains(timeline.Items, i => i.EventType == "ClinicalNote");
        Assert.Contains(timeline.Items, i => i.EventType == "Attachment");
        Assert.Contains(timeline.Items, i => i.EventType == "Consultation");

        // -------------------------------------------------------------
        // Step 11: Audit trail persistence check
        // -------------------------------------------------------------
        await using var verifyScope = application.Services.CreateAsyncScope();
        var auditDb = verifyScope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var auditEvents = await auditDb.AuditLogs.AsNoTracking().ToListAsync();

        Assert.Contains(auditEvents, a => a.Action == "Identity.Login");
        Assert.Contains(auditEvents, a => a.Action == "ClinicalRecords.EncounterStart");
        Assert.Contains(auditEvents, a => a.Action == "ClinicalRecords.VitalSignPanelRecord");
        Assert.Contains(auditEvents, a => a.Action == "ClinicalRecords.ClinicalNoteSign");
        Assert.Contains(auditEvents, a => a.Action == "ClinicalRecords.DiagnosisRecord");
        Assert.Contains(auditEvents, a => a.Action == "ClinicalRecords.AttachmentUpload");
        Assert.Contains(auditEvents, a => a.Action == "ClinicalRecords.ConsultationRequest");
        Assert.Contains(auditEvents, a => a.Action == "ClinicalRecords.EncounterComplete");
        Assert.Contains(auditEvents, a => a.Action == "ClinicalRecords.TimelineView");
        Assert.DoesNotContain(auditEvents, a =>
            (a.Reason ?? string.Empty).Contains("Göğüs sıkışması", StringComparison.OrdinalIgnoreCase)
            || (a.Reason ?? string.Empty).Contains("Penisilin", StringComparison.OrdinalIgnoreCase)
            || (a.Reason ?? string.Empty).Contains("İskemi bulgusu", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-KAPI")]
    public async Task Phase4GateRejectsStaleClinicalWritesAndAdministrativeIdor()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: new InMemoryIdentityMessageSender());

        await RunAllMigrationsAsync(application);
        await ClinicalTestData.SeedOrganizationAsync(application);
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();
        }

        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(application);
        var doctorClient = CreateSecureClient(application);
        var adminClient = CreateSecureClient(application);
        var patientClient = CreateSecureClient(application);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1")).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1")).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1")).StatusCode);

        var createResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/clinical-records/notes",
            new CreateClinicalNoteRequest
            {
                EncounterId = encounterId,
                PatientId = ClinicalTestData.DemoPatientPersonId,
                NoteType = "GeneralSoap",
                Title = "DEMO eşzamanlı düzenleme testi",
                Content = "İlk sürüm",
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var note = await createResponse.Content.ReadFromJsonAsync<ClinicalNoteResponse>();
        Assert.NotNull(note);

        var firstUpdate = new UpdateClinicalNoteDraftRequest
        {
            ExpectedVersion = note.Version,
            Title = note.Title,
            Content = "Birinci kullanıcının güncellemesi",
        };
        var firstUpdateResponse = await PutWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/notes/{note.Id}",
            firstUpdate);
        Assert.Equal(HttpStatusCode.OK, firstUpdateResponse.StatusCode);

        var staleUpdateResponse = await PutWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/notes/{note.Id}",
            firstUpdate with
            {
                Content = "Bayat sürümle kayıp güncelleme"
            });
        Assert.Equal(HttpStatusCode.Conflict, staleUpdateResponse.StatusCode);

        var currentResponse = await doctorClient.GetAsync($"/api/v1/clinical-records/notes/{note.Id}");
        Assert.Equal(HttpStatusCode.OK, currentResponse.StatusCode);
        var current = await currentResponse.Content.ReadFromJsonAsync<ClinicalNoteResponse>();
        Assert.NotNull(current);
        Assert.Equal("Birinci kullanıcının güncellemesi", current.Content);

        var adminResponse = await adminClient.GetAsync($"/api/v1/clinical-records/notes/{note.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, adminResponse.StatusCode);

        var patientDraftResponse = await patientClient.GetAsync($"/api/v1/clinical-records/notes/{note.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, patientDraftResponse.StatusCode);
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
            Content = JsonContent.Create(body),
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
    }
}
