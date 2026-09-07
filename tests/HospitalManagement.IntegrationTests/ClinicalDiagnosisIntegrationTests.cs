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

public sealed class ClinicalDiagnosisIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G05")]
    public async Task DoctorCanSearchCatalogAndRecordCodedAndFreeTextDiagnosesAndManageLifecycle()
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

        // 1. Search Catalog
        var searchResp = await doctorClient.GetAsync("/api/v1/clinical-records/diagnosis-catalog?query=tonsillit");
        Assert.Equal(HttpStatusCode.OK, searchResp.StatusCode);
        var catalogItems = await searchResp.Content.ReadFromJsonAsync<IReadOnlyList<DiagnosisCatalogItemResponse>>();
        Assert.NotNull(catalogItems);
        Assert.Contains(catalogItems, c => c.Code == "J03.9");

        // 2. Record Coded Diagnosis
        var codedReq = new CreateDiagnosisRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            DiagnosisType = "Final",
            IsCoded = true,
            Icd10Code = "J03.9",
            DiagnosisTitle = "Akut tonsillit, tanımlanmamış",
            Notes = "Klinik muayene ve orofarenks bulguları ile kesinleşti",
        };

        var codedResp = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/clinical-records/diagnoses",
            codedReq);

        Assert.Equal(HttpStatusCode.Created, codedResp.StatusCode);
        var codedDiagnosis = await codedResp.Content.ReadFromJsonAsync<EncounterDiagnosisResponse>();
        Assert.NotNull(codedDiagnosis);
        Assert.True(codedDiagnosis.IsCoded);
        Assert.Equal("J03.9", codedDiagnosis.Icd10Code);
        Assert.Equal("Final", codedDiagnosis.DiagnosisType);
        Assert.Equal("ICD-10-TR-2026.1", codedDiagnosis.CatalogVersion);

        // 3. Record Free-text Differential Diagnosis
        var freeTextReq = new CreateDiagnosisRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            DiagnosisType = "Differential",
            IsCoded = false,
            DiagnosisTitle = "Enfeksiyöz mononükleoz şüphesi",
            Notes = "Lenfadenopati eşlik ediyor",
        };

        var freeTextResp = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/clinical-records/diagnoses",
            freeTextReq);

        Assert.Equal(HttpStatusCode.Created, freeTextResp.StatusCode);
        var freeTextDiagnosis = await freeTextResp.Content.ReadFromJsonAsync<EncounterDiagnosisResponse>();
        Assert.NotNull(freeTextDiagnosis);
        Assert.False(freeTextDiagnosis.IsCoded);
        Assert.Null(freeTextDiagnosis.Icd10Code);

        var immutableFinalUpdate = await PutWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/diagnoses/{codedDiagnosis.Id}",
            new UpdateDiagnosisRequest
            {
                ExpectedVersion = codedDiagnosis.Version,
                DiagnosisType = "Final",
                IsCoded = true,
                Icd10Code = "J03.9",
                DiagnosisTitle = "Sessizce değiştirilemez",
            });
        Assert.Equal(HttpStatusCode.Conflict, immutableFinalUpdate.StatusCode);

        var diagnosisId = freeTextDiagnosis.Id;

        // 4. Update Diagnosis
        var updateReq = new UpdateDiagnosisRequest
        {
            ExpectedVersion = freeTextDiagnosis.Version,
            DiagnosisType = "Final",
            IsCoded = true,
            Icd10Code = "J03.9",
            DiagnosisTitle = "Akut tonsillit (Bakteriyel)",
            Notes = "Boğaz kültürü sonucu eklendi",
        };

        var updateResp = await PutWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/diagnoses/{diagnosisId}",
            updateReq);

        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updatedDiagnosis = await updateResp.Content.ReadFromJsonAsync<EncounterDiagnosisResponse>();
        Assert.NotNull(updatedDiagnosis);
        Assert.Equal("Akut tonsillit, tanımlanmamış", updatedDiagnosis.DiagnosisTitle);

        // 5. Mark Diagnosis Entered In Error
        var markErrorReq = new MarkDiagnosisEnteredInErrorRequest
        {
            ExpectedVersion = updatedDiagnosis.Version,
            Reason = "Yanlış hasta dosyasına girilen tanı",
        };

        var markErrorResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/diagnoses/{diagnosisId}/entered-in-error",
            markErrorReq);

        Assert.Equal(HttpStatusCode.OK, markErrorResp.StatusCode);
        var errorDiagnosis = await markErrorResp.Content.ReadFromJsonAsync<EncounterDiagnosisResponse>();
        Assert.NotNull(errorDiagnosis);
        Assert.True(errorDiagnosis.IsEnteredInError);

        // 6. Updating an entered-in-error diagnosis is locked (409 Conflict)
        var conflictResp = await PutWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-records/diagnoses/{diagnosisId}",
            updateReq with
            {
                ExpectedVersion = errorDiagnosis.Version
            });

        Assert.Equal(HttpStatusCode.Conflict, conflictResp.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G05")]
    public async Task PatientCanViewOwnDiagnosesAndOtherPatientAccessIsForbidden()
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

        // 1. Patient views own diagnoses -> 200 OK
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var patientResponse = await patientClient.GetAsync($"/api/v1/clinical-records/diagnoses/by-patient/{patientId}");
        Assert.Equal(HttpStatusCode.OK, patientResponse.StatusCode);

        // 2. Patient tries to view another patient's diagnoses -> 403 Forbidden
        var otherPatientId = Guid.NewGuid();
        var forbiddenResponse = await patientClient.GetAsync($"/api/v1/clinical-records/diagnoses/by-patient/{otherPatientId}");
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
