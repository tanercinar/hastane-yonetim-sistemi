using System.Net;
using System.Net.Http.Json;

using HospitalManagement.Contracts.ClinicalRecords;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
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

public sealed class ClinicalNotesIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G04")]
    public async Task DoctorCanCreateDraftUpdateSignAndAddAddendumToClinicalNote()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAsync(application);

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();
        }

        var doctorClient = CreateSecureClient(application);
        var loginResp = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(application);

        // 1. Create Draft Note
        var draftRequest = new CreateClinicalNoteRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            NoteType = "GeneralSoap",
            Title = "Genel Poliklinik Muayenesi",
            ChiefComplaint = "Baş ağrısı ve göz arkasında zonklama",
            HistoryOfPresentIllness = "2 gündür süren şakak bölgesinde zonklayıcı baş ağrısı",
            PhysicalExamination = "Nörolojik muayene doğal, meningeal irritasyon bulgusu yok",
            Assessment = "Gerilim tipi baş ağrısı / Migren",
            Plan = "İstirahat ve analjezik tedavi",
            Content = "Hasta 1 hafta sonra baş ağrısı günlüğü ile kontrole çağrıldı.",
        };

        var draftResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/clinical-records/notes",
            draftRequest);

        Assert.Equal(HttpStatusCode.Created, draftResponse.StatusCode);
        var note = await draftResponse.Content.ReadFromJsonAsync<ClinicalNoteResponse>();
        Assert.NotNull(note);
        Assert.Equal("Draft", note.Status);
        Assert.Equal("Genel Poliklinik Muayenesi", note.Title);

        var noteId = note.Id;

        // 2. Update Draft Note
        var updateRequest = new UpdateClinicalNoteDraftRequest
        {
            ExpectedVersion = note.Version,
            Title = "Genel Poliklinik Muayenesi (Revize)",
            ChiefComplaint = "Baş ağrısı, bulantı",
            HistoryOfPresentIllness = "2 gündür süren baş ağrısı ve hafif bulantı",
            PhysicalExamination = "Nörolojik muayene doğal",
            Assessment = "Migren atağı",
            Plan = "Triptan + hidrasyon",
            Content = "İlaç reçete edildi.",
        };

        var updateResponse = await PutWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/notes/{noteId}",
            updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedNote = await updateResponse.Content.ReadFromJsonAsync<ClinicalNoteResponse>();
        Assert.NotNull(updatedNote);
        Assert.Equal("Genel Poliklinik Muayenesi (Revize)", updatedNote.Title);
        Assert.Equal("Draft", updatedNote.Status);

        // 3. Sign Note
        var signRequest = new SignClinicalNoteRequest
        {
            ExpectedVersion = updatedNote.Version,
            SignatureNote = "Muayene tamamlandı ve onaylandı",
        };

        var signResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/notes/{noteId}/sign",
            signRequest);

        Assert.Equal(HttpStatusCode.OK, signResponse.StatusCode);
        var signedNote = await signResponse.Content.ReadFromJsonAsync<ClinicalNoteResponse>();
        Assert.NotNull(signedNote);
        Assert.Equal("Signed", signedNote.Status);
        Assert.NotNull(signedNote.SignedAtUtc);

        // 4. Updating Signed Note is Forbidden (409 Conflict)
        var forbiddenUpdateResponse = await PutWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/notes/{noteId}",
            updateRequest with
            {
                ExpectedVersion = signedNote.Version
            });

        Assert.Equal(HttpStatusCode.Conflict, forbiddenUpdateResponse.StatusCode);

        // 5. Add Addendum to Signed Note
        var addendumRequest = new AddClinicalNoteAddendumRequest
        {
            ExpectedVersion = signedNote.Version,
            AddendumContent = "Hastanın kraniyal MRG randevusu planlandı.",
            Reason = "İleri tetkik planı eklendi",
        };

        var addendumResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/notes/{noteId}/addendum",
            addendumRequest);

        Assert.Equal(HttpStatusCode.Created, addendumResponse.StatusCode);
        var addendumNote = await addendumResponse.Content.ReadFromJsonAsync<ClinicalNoteResponse>();
        Assert.NotNull(addendumNote);
        Assert.Equal("Addendum", addendumNote.NoteType);
        Assert.Equal("Signed", addendumNote.Status);
        Assert.Equal(noteId, addendumNote.ParentNoteId);
        Assert.Equal("İleri tetkik planı eklendi", addendumNote.CorrectionReason);

        // 6. Check that original note status is now Amended
        var getOriginalResponse = await doctorClient.GetAsync($"/api/v1/clinical-records/notes/{noteId}");
        Assert.Equal(HttpStatusCode.OK, getOriginalResponse.StatusCode);
        var refreshedOriginal = await getOriginalResponse.Content.ReadFromJsonAsync<ClinicalNoteResponse>();
        Assert.NotNull(refreshedOriginal);
        Assert.Equal("Amended", refreshedOriginal.Status);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G04")]
    public async Task NurseCanCreateNurseNoteAndPatientCanViewOwnNotes()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAsync(application);

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();
        }

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(
            application,
            additionalPractitionerIds: [ClinicalTestData.DemoNursePersonId]);

        // 1. Nurse creates nurse note
        var nurseClient = CreateSecureClient(application);
        await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");

        var nurseNoteRequest = new CreateClinicalNoteRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            NoteType = "NurseNote",
            Title = "Hemşire Triyaj ve Bakım Notu",
            Content = "Hasta poliklinik girişinde karşılandı, vital bulgular stabil.",
        };

        var nurseNoteResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            "/api/v1/clinical-records/notes",
            nurseNoteRequest);

        Assert.Equal(HttpStatusCode.Created, nurseNoteResponse.StatusCode);
        var createdNurseNote = await nurseNoteResponse.Content.ReadFromJsonAsync<ClinicalNoteResponse>();
        Assert.NotNull(createdNurseNote);
        Assert.Equal("NurseNote", createdNurseNote.NoteType);

        var signResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/clinical-records/notes/{createdNurseNote.Id}/sign",
            new SignClinicalNoteRequest { ExpectedVersion = createdNurseNote.Version });
        Assert.Equal(HttpStatusCode.OK, signResponse.StatusCode);

        // 2. Patient views own notes -> 200 OK
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var patientResponse = await patientClient.GetAsync($"/api/v1/clinical-records/notes/by-patient/{patientId}");
        Assert.Equal(HttpStatusCode.OK, patientResponse.StatusCode);
        var patientNotes = await patientResponse.Content.ReadFromJsonAsync<IReadOnlyList<ClinicalNoteResponse>>();
        Assert.NotNull(patientNotes);
        Assert.Contains(patientNotes, n => n.Id == createdNurseNote.Id);

        // 3. Patient tries to view another patient's notes -> 403 Forbidden
        var otherPatientId = Guid.NewGuid();
        var forbiddenResponse = await patientClient.GetAsync($"/api/v1/clinical-records/notes/by-patient/{otherPatientId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
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
