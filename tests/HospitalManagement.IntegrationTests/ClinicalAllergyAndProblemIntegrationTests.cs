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

public sealed class ClinicalAllergyAndProblemIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G02")]
    public async Task DoctorCanRecordAllergyAndUpdateStatusAndMarkEnteredInError()
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

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        await ClinicalTestData.SeedStartedEncounterAsync(application, patientId);

        // 1. Create Allergy
        var createRequest = new CreateAllergyRequest
        {
            PatientId = patientId,
            Substance = "Penisilin",
            Category = "Medication",
            Criticality = "High",
            Manifestation = "Döküntü ve nefes darlığı",
            OnsetDateTimeUtc = DateTime.UtcNow.AddYears(-1),
            Notes = "İlk kullanımda reaksiyon verdi",
        };

        var createResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/clinical-records/allergies",
            createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var allergy = await createResponse.Content.ReadFromJsonAsync<AllergyResponse>();
        Assert.NotNull(allergy);
        Assert.Equal("Penisilin", allergy.Substance);
        Assert.Equal("Active", allergy.ClinicalStatus);
        Assert.Equal("Confirmed", allergy.VerificationStatus);

        var allergyId = allergy.Id;

        // 2. Update Status
        var updateRequest = new UpdateAllergyStatusRequest
        {
            ExpectedVersion = allergy.Version,
            ClinicalStatus = "Inactive",
            Notes = "Takip altında pasifize edildi",
        };

        var updateResponse = await PatchWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/allergies/{allergyId}/status",
            updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedAllergy = await updateResponse.Content.ReadFromJsonAsync<AllergyResponse>();
        Assert.NotNull(updatedAllergy);
        Assert.Equal("Inactive", updatedAllergy.ClinicalStatus);

        // 3. Mark Entered In Error
        var markErrorRequest = new MarkAllergyEnteredInErrorRequest
        {
            ExpectedVersion = updatedAllergy.Version,
            Reason = "Farklı etken madde ile karıştırıldı",
        };

        var errorResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/allergies/{allergyId}/entered-in-error",
            markErrorRequest);

        Assert.Equal(HttpStatusCode.OK, errorResponse.StatusCode);
        var errorAllergy = await errorResponse.Content.ReadFromJsonAsync<AllergyResponse>();
        Assert.NotNull(errorAllergy);
        Assert.Equal("EnteredInError", errorAllergy.VerificationStatus);
        Assert.Equal("Farklı etken madde ile karıştırıldı", errorAllergy.EnteredInErrorReason);

        // 4. Get Patient Allergies
        var getResponse = await doctorClient.GetAsync($"/api/v1/clinical-records/allergies/by-patient/{patientId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var allergies = await getResponse.Content.ReadFromJsonAsync<IReadOnlyList<AllergyResponse>>();
        Assert.NotNull(allergies);
        Assert.Contains(allergies, a => a.Id == allergyId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G02")]
    public async Task DoctorCanRecordProblemAndResolveAndPatientCanView()
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
        await ClinicalTestData.SeedStartedEncounterAsync(application, patientId);

        // 1. Create Problem
        var createRequest = new CreateClinicalProblemRequest
        {
            PatientId = patientId,
            ProblemTitle = "Akut Bronşit",
            Code = "J20.9",
            Category = "ActiveProblem",
            OnsetDate = new DateOnly(2026, 8, 20),
            Notes = "Öksürük ve balgam mevcut",
        };

        var createResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/clinical-records/problems",
            createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var problem = await createResponse.Content.ReadFromJsonAsync<ClinicalProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Akut Bronşit", problem.ProblemTitle);
        Assert.Equal("Active", problem.ClinicalStatus);

        var problemId = problem.Id;

        // 2. Resolve Problem
        var resolveRequest = new UpdateClinicalProblemStatusRequest
        {
            ExpectedVersion = problem.Version,
            ClinicalStatus = "Resolved",
            ResolvedDate = new DateOnly(2026, 8, 28),
            Notes = "Tedavi tamamlandı, semptomlar geriledi",
        };

        var resolveResponse = await PatchWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/problems/{problemId}/status",
            resolveRequest);

        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);
        var resolvedProblem = await resolveResponse.Content.ReadFromJsonAsync<ClinicalProblemResponse>();
        Assert.NotNull(resolvedProblem);
        Assert.Equal("Resolved", resolvedProblem.ClinicalStatus);
        Assert.Equal(new DateOnly(2026, 8, 28), resolvedProblem.ResolvedDate);

        // 3. Patient Views Own Problems
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var patientGetResponse = await patientClient.GetAsync($"/api/v1/clinical-records/problems/by-patient/{patientId}");
        Assert.Equal(HttpStatusCode.OK, patientGetResponse.StatusCode);
        var patientProblems = await patientGetResponse.Content.ReadFromJsonAsync<IReadOnlyList<ClinicalProblemResponse>>();
        Assert.NotNull(patientProblems);
        Assert.Contains(patientProblems, p => p.Id == problemId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G02")]
    public async Task UnauthorizedPatientCannotRecordAllergyOrAccessOtherPatientData()
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

        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var otherPatientId = Guid.NewGuid();

        // 1. Patient trying to create allergy -> 403 Forbidden
        var createResponse = await PostWithAntiforgeryAsync(
            patientClient,
            "/api/v1/clinical-records/allergies",
            new CreateAllergyRequest
            {
                PatientId = otherPatientId,
                Substance = "Fıstık",
            });

        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);

        // 2. Patient trying to view another patient's problems -> 403 Forbidden
        var getResponse = await patientClient.GetAsync($"/api/v1/clinical-records/problems/by-patient/{otherPatientId}");
        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);
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

    private static async Task<HttpResponseMessage> PatchWithAntiforgeryAsync<TRequest>(
        HttpClient client,
        string requestUri,
        TRequest body)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/v1/identity/antiforgery");
        Assert.NotNull(token);
        using var request = new HttpRequestMessage(HttpMethod.Patch, requestUri)
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
