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

public sealed class ClinicalConsultationIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G06")]
    public async Task DoctorCanRequestAcceptAndCompleteConsultationWorkflow()
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
        var targetDepartmentId = ClinicalTestData.DemoCardiologyDepartmentId;
        var chiefClient = CreateSecureClient(application);
        var chiefLogin = await LoginAsync(chiefClient, "DEMO-chief@hospital.invalid", "DEMO-Chief-Pass!1");
        Assert.Equal(HttpStatusCode.OK, chiefLogin.StatusCode);

        // 1. Request Consultation
        var requestPayload = new CreateConsultationRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            TargetDepartmentId = targetDepartmentId,
            TargetPractitionerId = ClinicalTestData.DemoChiefPersonId,
            Urgency = "Urgent",
            ReasonForConsultation = "Göğüs ağrısı ayırıcı tanısı",
            ClinicalQuestion = "Troponin yüksekliği ve EKG değişikliği açısından değerlendiriniz.",
        };

        var requestResp = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/clinical-records/consultations",
            requestPayload);

        Assert.Equal(HttpStatusCode.Created, requestResp.StatusCode);
        var created = await requestResp.Content.ReadFromJsonAsync<ConsultationResponse>();
        Assert.NotNull(created);
        Assert.Equal("Requested", created.Status);
        Assert.Equal("Urgent", created.Urgency);

        var consultationId = created.Id;

        // 2. Accept Consultation
        var acceptResp = await PostWithAntiforgeryAsync(
            chiefClient,
            $"/api/v1/clinical-records/consultations/{consultationId}/accept",
            new AcceptConsultationRequest
            {
                ExpectedVersion = created.Version,
                Notes = "Konsültasyon alındı.",
            });

        Assert.Equal(HttpStatusCode.OK, acceptResp.StatusCode);
        var accepted = await acceptResp.Content.ReadFromJsonAsync<ConsultationResponse>();
        Assert.NotNull(accepted);
        Assert.Equal("Accepted", accepted.Status);
        Assert.NotNull(accepted.AcceptedAtUtc);

        // 3. Complete Consultation
        var completePayload = new CompleteConsultationRequest
        {
            ExpectedVersion = accepted.Version,
            ConsultationReport = "EKG'de patoloji saptanmadı. Troponin kontrolü normal.",
            Recommendation = "Kardiyoloji açısından taburculuğuna engel yoktur.",
        };

        var completeResp = await PostWithAntiforgeryAsync(
            chiefClient,
            $"/api/v1/clinical-records/consultations/{consultationId}/complete",
            completePayload);

        Assert.Equal(HttpStatusCode.OK, completeResp.StatusCode);
        var completed = await completeResp.Content.ReadFromJsonAsync<ConsultationResponse>();
        Assert.NotNull(completed);
        Assert.Equal("Completed", completed.Status);
        Assert.NotNull(completed.CompletedAtUtc);
        Assert.Equal("EKG'de patoloji saptanmadı. Troponin kontrolü normal.", completed.ConsultationReport);

        // 4. Get by Id
        var getResp = await doctorClient.GetAsync($"/api/v1/clinical-records/consultations/{consultationId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var fetched = await getResp.Content.ReadFromJsonAsync<ConsultationResponse>();
        Assert.NotNull(fetched);
        Assert.Equal("Completed", fetched.Status);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G06")]
    public async Task PatientCanViewOwnConsultationsAndOtherPatientAccessIsForbidden()
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

        // 1. Patient views own consultations -> 200 OK
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var patientResponse = await patientClient.GetAsync($"/api/v1/clinical-records/consultations/by-patient/{patientId}");
        Assert.Equal(HttpStatusCode.OK, patientResponse.StatusCode);

        // 2. Patient tries to view another patient's consultations -> 403 Forbidden
        var otherPatientId = Guid.NewGuid();
        var forbiddenResponse = await patientClient.GetAsync($"/api/v1/clinical-records/consultations/by-patient/{otherPatientId}");
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
