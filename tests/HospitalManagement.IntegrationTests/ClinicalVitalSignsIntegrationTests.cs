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

public sealed class ClinicalVitalSignsIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G03")]
    public async Task NurseCanRecordVitalSignsPanelAndSingleObservationAndMarkEnteredInError()
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

        var nurseClient = CreateSecureClient(application);
        var loginResp = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        await ClinicalTestData.SeedStartedEncounterAsync(
            application,
            patientId,
            [ClinicalTestData.DemoNursePersonId]);

        // 1. Record Panel
        var panelRequest = new RecordVitalSignsPanelRequest
        {
            PatientId = patientId,
            TemperatureCelsius = 37.2m,
            SystolicBloodPressureMmHg = 120,
            DiastolicBloodPressureMmHg = 80,
            HeartRateBpm = 76,
            RespiratoryRatePerMin = 16,
            OxygenSaturationPercent = 98m,
            BodyWeightKg = 70.5m,
            BodyHeightCm = 175m,
            BloodGlucoseMgDl = 95m,
            PainScore = 1,
            ConsciousnessState = "Alert",
            Notes = "Genel durumu iyi, oryante ve koopere",
        };

        var panelResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            "/api/v1/clinical-records/vital-signs/panel",
            panelRequest);

        Assert.Equal(HttpStatusCode.Created, panelResponse.StatusCode);
        var panel = await panelResponse.Content.ReadFromJsonAsync<VitalSignsPanelResponse>();
        Assert.NotNull(panel);
        Assert.True(panel.Observations.Count >= 10);
        // Includes BMI calculation
        Assert.Contains(panel.Observations, o => o.MeasurementType == "BodyMassIndex");

        // 2. Record Single Vital
        var singleRequest = new CreateVitalSignObservationRequest
        {
            PatientId = patientId,
            MeasurementType = "HeartRate",
            Value = 82m,
            Unit = "bpm",
            MeasurementMethod = "Monitörizasyon",
            Notes = "İkinci ölçüm",
        };

        var singleResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            "/api/v1/clinical-records/vital-signs",
            singleRequest);

        Assert.Equal(HttpStatusCode.Created, singleResponse.StatusCode);
        var singleObservation = await singleResponse.Content.ReadFromJsonAsync<VitalSignObservationResponse>();
        Assert.NotNull(singleObservation);
        Assert.Equal("HeartRate", singleObservation.MeasurementType);
        Assert.Equal(82m, singleObservation.Value);

        var observationId = singleObservation.Id;

        // 3. Mark Entered In Error
        var markErrorRequest = new MarkVitalSignEnteredInErrorRequest
        {
            ExpectedVersion = singleObservation.Version,
            Reason = "Hasta hareket ettiği için artefakt oluştu",
        };

        var markErrorResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/clinical-records/vital-signs/{observationId}/entered-in-error",
            markErrorRequest);

        Assert.Equal(HttpStatusCode.OK, markErrorResponse.StatusCode);
        var errorObservation = await markErrorResponse.Content.ReadFromJsonAsync<VitalSignObservationResponse>();
        Assert.NotNull(errorObservation);
        Assert.True(errorObservation.IsEnteredInError);
        Assert.Equal("Hasta hareket ettiği için artefakt oluştu", errorObservation.EnteredInErrorReason);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G03")]
    public async Task ValidationRejectsPhysiologicallyImpossibleVitalValues()
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

        var nurseClient = CreateSecureClient(application);
        await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        await ClinicalTestData.SeedStartedEncounterAsync(
            application,
            patientId,
            [ClinicalTestData.DemoNursePersonId]);

        var invalidRequest = new CreateVitalSignObservationRequest
        {
            PatientId = patientId,
            MeasurementType = "BodyTemperature",
            Value = 65m, // Impossible human temp
            Unit = "°C",
        };

        var response = await PostWithAntiforgeryAsync(
            nurseClient,
            "/api/v1/clinical-records/vital-signs",
            invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G03")]
    public async Task DoctorAndPatientCanViewVitalsAndOtherPatientAccessIsForbidden()
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
        await ClinicalTestData.SeedStartedEncounterAsync(application, patientId);

        // 1. Doctor views vitals -> 200 OK
        var doctorClient = CreateSecureClient(application);
        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");

        var docResponse = await doctorClient.GetAsync($"/api/v1/clinical-records/vital-signs/by-patient/{patientId}");
        Assert.Equal(HttpStatusCode.OK, docResponse.StatusCode);

        // 2. Patient views own vitals -> 200 OK
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var patientResponse = await patientClient.GetAsync($"/api/v1/clinical-records/vital-signs/by-patient/{patientId}");
        Assert.Equal(HttpStatusCode.OK, patientResponse.StatusCode);

        // 3. Patient tries to view another patient's vitals -> 403 Forbidden
        var otherPatientId = Guid.NewGuid();
        var forbiddenResponse = await patientClient.GetAsync($"/api/v1/clinical-records/vital-signs/by-patient/{otherPatientId}");
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
