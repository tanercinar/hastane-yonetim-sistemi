using System.Net;
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

public sealed class ClinicalEncounterIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G01")]
    public async Task EncounterLifecycleHappyPathSupportsStartAddParticipantAndComplete()
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

        var loginResp = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var patientId = ClinicalTestData.DemoPatientPersonId;
        var departmentId = ClinicalTestData.DemoCardiologyDepartmentId;
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var appointmentId = Guid.NewGuid();

        // 1. Create Encounter
        var createRequest = new CreateEncounterRequest
        {
            AppointmentId = appointmentId,
            PatientId = patientId,
            DepartmentId = departmentId,
            PrimaryPractitionerId = doctorId,
            EncounterType = "Outpatient",
            PlannedStartTimeUtc = DateTime.UtcNow.AddMinutes(10),
            ChiefComplaint = "Göğüs ağrısı ve nefes darlığı",
        };

        var createResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/clinical-records/encounters",
            createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var encounter = await createResponse.Content.ReadFromJsonAsync<EncounterDetailResponse>();
        Assert.NotNull(encounter);
        Assert.Equal("Planned", encounter.Status);
        Assert.Equal(patientId, encounter.PatientId);
        Assert.Equal(appointmentId, encounter.AppointmentId);
        Assert.Equal("Göğüs ağrısı ve nefes darlığı", encounter.ChiefComplaint);
        Assert.Single(encounter.Participants);

        var encounterId = encounter.Id;

        // 2. Start Encounter
        var startResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/encounters/{encounterId}/start",
            new StartEncounterRequest
            {
                ExpectedVersion = encounter.Version,
                StartTimeUtc = DateTime.UtcNow,
            });

        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        var startedEncounter = await startResponse.Content.ReadFromJsonAsync<EncounterDetailResponse>();
        Assert.NotNull(startedEncounter);
        Assert.Equal("InProgress", startedEncounter.Status);
        Assert.NotNull(startedEncounter.ActualStartTimeUtc);

        // 3. Add Assisting Nurse as Participant
        var nurseId = Guid.Parse("00000000-0000-0000-0000-000000000103");
        var addParticipantResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/encounters/{encounterId}/participants",
            new AddEncounterParticipantRequest
            {
                ExpectedVersion = startedEncounter.Version,
                PractitionerId = nurseId,
                Role = "AssistingNurse",
            });

        Assert.Equal(HttpStatusCode.OK, addParticipantResponse.StatusCode);
        var updatedEncounter = await addParticipantResponse.Content.ReadFromJsonAsync<EncounterDetailResponse>();
        Assert.NotNull(updatedEncounter);
        Assert.Equal(2, updatedEncounter.Participants.Count);
        Assert.Contains(updatedEncounter.Participants, p => p.PractitionerId == nurseId && p.Role == "AssistingNurse");

        // 3.1 Record Diagnosis to satisfy encounter completeness rule
        var diagReq = new CreateDiagnosisRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            DiagnosisType = "Final",
            IsCoded = true,
            Icd10Code = "I10",
            DiagnosisTitle = "Esansiyel hipertansiyon",
        };
        var diagResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-records/diagnoses", diagReq);
        Assert.Equal(HttpStatusCode.Created, diagResp.StatusCode);

        // 4. Complete Encounter
        var completeResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/encounters/{encounterId}/complete",
            new CompleteEncounterRequest
            {
                ExpectedVersion = updatedEncounter.Version,
                EndTimeUtc = DateTime.UtcNow,
            });

        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completedEncounter = await completeResponse.Content.ReadFromJsonAsync<EncounterDetailResponse>();
        Assert.NotNull(completedEncounter);
        Assert.Equal("Completed", completedEncounter.Status);
        Assert.NotNull(completedEncounter.ActualEndTimeUtc);

        // 5. Get Encounter by ID
        var getResponse = await doctorClient.GetAsync($"/api/v1/clinical-records/encounters/{encounterId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetchedEncounter = await getResponse.Content.ReadFromJsonAsync<EncounterDetailResponse>();
        Assert.NotNull(fetchedEncounter);
        Assert.Equal("Completed", fetchedEncounter.Status);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G09")]
    public async Task EncounterCompletionRequiresDocumentationAndReopenRequiresPrivilegedMfaSession()
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
        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");

        var patientId = ClinicalTestData.DemoPatientPersonId;
        var departmentId = ClinicalTestData.DemoCardiologyDepartmentId;
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");

        // 1. Create and Start Encounter
        var createReq = new CreateEncounterRequest
        {
            PatientId = patientId,
            DepartmentId = departmentId,
            PrimaryPractitionerId = doctorId,
            EncounterType = "Outpatient",
            PlannedStartTimeUtc = DateTime.UtcNow,
        };
        var createResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-records/encounters", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var encounter = await createResp.Content.ReadFromJsonAsync<EncounterDetailResponse>();
        Assert.NotNull(encounter);

        var encounterId = encounter.Id;
        var startResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/encounters/{encounterId}/start",
            new StartEncounterRequest { ExpectedVersion = encounter.Version });
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        var started = await startResponse.Content.ReadFromJsonAsync<EncounterDetailResponse>();
        Assert.NotNull(started);

        // 2. Try to complete empty encounter -> 400 Bad Request (Completeness validation failed)
        var completeEmptyResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/encounters/{encounterId}/complete",
            new CompleteEncounterRequest { ExpectedVersion = started.Version });
        Assert.Equal(HttpStatusCode.BadRequest, completeEmptyResp.StatusCode);

        // 3. Create a Draft Note and try to complete -> 400 Bad Request (Draft note exists)
        var draftNoteReq = new CreateClinicalNoteRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            NoteType = "GeneralSoap",
            Title = "Genel Muayene",
            Assessment = "Değerlendirme metni",
        };
        var draftNoteResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-records/notes", draftNoteReq);
        Assert.Equal(HttpStatusCode.Created, draftNoteResp.StatusCode);
        var note = await draftNoteResp.Content.ReadFromJsonAsync<ClinicalNoteResponse>();
        Assert.NotNull(note);

        var completeWithDraftResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/encounters/{encounterId}/complete",
            new CompleteEncounterRequest { ExpectedVersion = started.Version });
        Assert.Equal(HttpStatusCode.BadRequest, completeWithDraftResp.StatusCode);

        // 4. Sign the Note
        var signResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/notes/{note.Id}/sign",
            new SignClinicalNoteRequest { ExpectedVersion = note.Version });
        Assert.Equal(HttpStatusCode.OK, signResp.StatusCode);

        // 5. Complete Encounter succeeds now!
        var completeSuccessResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/encounters/{encounterId}/complete",
            new CompleteEncounterRequest { ExpectedVersion = started.Version });
        Assert.Equal(HttpStatusCode.OK, completeSuccessResp.StatusCode);
        var completed = await completeSuccessResp.Content.ReadFromJsonAsync<EncounterDetailResponse>();
        Assert.NotNull(completed);
        Assert.Equal("Completed", completed.Status);

        // 6. A regular doctor cannot reopen, regardless of supplied reason.
        var reopenNoReasonResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/encounters/{encounterId}/reopen",
            new ReopenEncounterRequest
            {
                ExpectedVersion = completed.Version,
                Reason = "",
            });
        Assert.Equal(HttpStatusCode.Forbidden, reopenNoReasonResp.StatusCode);

        // 7. Reopen is reserved for a recent MFA-authenticated CHM session.
        var reopenResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/encounters/{encounterId}/reopen",
            new ReopenEncounterRequest
            {
                ExpectedVersion = completed.Version,
                Reason = "Laboratuvar sonucu incelenerek tanı revize edilecek",
            });
        Assert.Equal(HttpStatusCode.Forbidden, reopenResp.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G01")]
    public async Task DuplicateActiveEncounterForSameAppointmentReturnsConflict()
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

        var loginResp = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var appointmentId = Guid.NewGuid();
        var patientId = ClinicalTestData.DemoPatientPersonId;
        var departmentId = ClinicalTestData.DemoCardiologyDepartmentId;
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");

        var request1 = new CreateEncounterRequest
        {
            AppointmentId = appointmentId,
            PatientId = patientId,
            DepartmentId = departmentId,
            PrimaryPractitionerId = doctorId,
            EncounterType = "Outpatient",
            PlannedStartTimeUtc = DateTime.UtcNow,
        };

        var response1 = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/clinical-records/encounters",
            request1);

        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        // Attempt second encounter creation for same appointment
        var request2 = new CreateEncounterRequest
        {
            AppointmentId = appointmentId,
            PatientId = patientId,
            DepartmentId = departmentId,
            PrimaryPractitionerId = doctorId,
            EncounterType = "Outpatient",
            PlannedStartTimeUtc = DateTime.UtcNow,
        };

        var response2 = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/clinical-records/encounters",
            request2);

        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G01")]
    public async Task UnauthorizedPatientCannotCreateOrCompleteEncounter()
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

        var patientClient = CreateSecureClient(application);

        var loginResp = await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var createRequest = new CreateEncounterRequest
        {
            PatientId = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            PrimaryPractitionerId = Guid.NewGuid(),
        };

        // Patient attempting to create encounter -> 403 Forbidden
        var createResponse = await PostWithAntiforgeryAsync(
            patientClient,
            "/api/v1/clinical-records/encounters",
            createRequest);

        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
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
    }
}
