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

public sealed class ClinicalTimelineIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G08")]
    public async Task DoctorAndPatientCanViewAggregatedChronologicalTimelineWithPrivacyRules()
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
        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(application);

        // 1. Doctor records vital sign
        var vitalReq = new CreateVitalSignObservationRequest
        {
            PatientId = patientId,
            EncounterId = encounterId,
            MeasurementType = "HeartRate",
            Value = 78,
            Unit = "bpm",
            MeasuredAtUtc = DateTime.UtcNow.AddHours(-3),
        };
        var vitalResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-records/vital-signs", vitalReq);
        Assert.Equal(HttpStatusCode.Created, vitalResp.StatusCode);

        // 2. Doctor records diagnosis
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

        // 3. Doctor creates draft note (not signed yet)
        var draftReq = new CreateClinicalNoteRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            NoteType = "GeneralSoap",
            Title = "Gizli Taslak Not",
            Content = "Taslak aşamasındaki klinik görüş",
        };
        var draftResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-records/notes", draftReq);
        Assert.Equal(HttpStatusCode.Created, draftResp.StatusCode);
        var draftNote = await draftResp.Content.ReadFromJsonAsync<ClinicalNoteResponse>();
        Assert.NotNull(draftNote);

        // 4. Doctor views timeline -> Contains VitalSigns, Diagnosis and Draft Note
        var docTimelineResp = await doctorClient.GetAsync($"/api/v1/clinical-records/timeline/by-patient/{patientId}?pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, docTimelineResp.StatusCode);
        var docTimeline = await docTimelineResp.Content.ReadFromJsonAsync<PatientTimelinePagedResponse>();
        Assert.NotNull(docTimeline);
        Assert.True(docTimeline.TotalCount >= 3);
        Assert.Contains(docTimeline.Items, i => i.EventType == "ClinicalNote" && i.EventId == draftNote.Id);
        Assert.DoesNotContain(docTimeline.Items, i => i.Title.Contains("Gizli Taslak", StringComparison.Ordinal));
        Assert.DoesNotContain(docTimeline.Items, i => i.Summary.Contains("klinik görüş", StringComparison.OrdinalIgnoreCase));

        // 5. Patient logs in and views own timeline -> Draft note is filtered out!
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var patTimelineResp = await patientClient.GetAsync($"/api/v1/clinical-records/timeline/by-patient/{patientId}?pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, patTimelineResp.StatusCode);
        var patTimeline = await patTimelineResp.Content.ReadFromJsonAsync<PatientTimelinePagedResponse>();
        Assert.NotNull(patTimeline);
        Assert.DoesNotContain(patTimeline.Items, i => i.EventId == draftNote.Id);
        Assert.Contains(patTimeline.Items, i => i.EventType == "Diagnosis");

        // 6. Patient attempts to access another patient's timeline -> 403 Forbidden
        var otherPatientId = Guid.NewGuid();
        var forbiddenResp = await patientClient.GetAsync($"/api/v1/clinical-records/timeline/by-patient/{otherPatientId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResp.StatusCode);
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
