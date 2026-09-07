using System.Net;
using System.Net.Http.Json;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Patients;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Application;
using HospitalManagement.Modules.Patients.Infrastructure;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class PatientSearchAndRegistrationIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F03-G02")]
    public async Task RegistrationStaffSearchWithPagingAndAuditing()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        // Run migrations
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identityDb = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
            await identityDb.Database.MigrateAsync();
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            await auditDb.Database.MigrateAsync();
            var patientsDb = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();
            await patientsDb.Database.MigrateAsync();
        }

        // Seed demo identity and patient data
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();

            var patientSeeder = scope.ServiceProvider.GetRequiredService<IPatientDataSeeder>();
            await patientSeeder.SeedAsync();
        }

        using var client = CreateSecureClient(application);

        var regDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.RegistrationStaff);

        // 1. Giriş yap
        var loginResp = await LoginAsync(client, regDef.Email, regDef.Password);
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        // 2. Arama: Boş sorgu ile tüm hastaları sayfalama ile listeleme
        var page1Resp = await client.GetAsync("/api/v1/patients?page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, page1Resp.StatusCode);
        var page1Data = await page1Resp.Content.ReadFromJsonAsync<PatientListResponse>();
        Assert.NotNull(page1Data);
        Assert.Single(page1Data.Items);
        Assert.True(page1Data.TotalCount >= 2);

        // 3. Arama: İsim filtresi ("Ayşe")
        var searchResp = await client.GetAsync("/api/v1/patients?query=Ayşe&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, searchResp.StatusCode);
        var searchData = await searchResp.Content.ReadFromJsonAsync<PatientListResponse>();
        Assert.NotNull(searchData);
        Assert.Contains(searchData.Items, p => p.FirstName == "Ayşe");
        Assert.All(searchData.Items, p =>
        {
            Assert.NotNull(p.MaskedNationalId);
            Assert.Contains("*", p.MaskedNationalId);
        });

        // 4. Mükerrer Kontrolü API'si
        var dupResp = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/patients/duplicate-check",
            new DuplicatePatientCheckRequest
            {
                FirstName = "Ayşe",
                LastName = "Yılmaz",
                DateOfBirth = new DateOnly(1985, 4, 15),
            });
        Assert.Equal(HttpStatusCode.OK, dupResp.StatusCode);
        var dupData = await dupResp.Content.ReadFromJsonAsync<DuplicateCheckResponse>();
        Assert.NotNull(dupData);
        Assert.True(dupData.HasPotentialDuplicate);
        Assert.NotEmpty(dupData.Candidates);

        // 5. İki kayıt görevlisinin eşzamanlı kaydı benzersiz MRN üretmelidir.
        using var secondClient = CreateSecureClient(application);
        var secondLogin = await LoginAsync(secondClient, regDef.Email, regDef.Password);
        Assert.Equal(HttpStatusCode.OK, secondLogin.StatusCode);

        var firstRegistrationTask = PostWithAntiforgeryAsync(
            client,
            "/api/v1/patients",
            new PatientRegistrationRequest
            {
                FirstName = "DEMO-Eşzamanlı-A",
                LastName = "Hasta",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Gender = "Female",
                Email = "DEMO-concurrent-a@hospital.invalid",
            });
        var secondRegistrationTask = PostWithAntiforgeryAsync(
            secondClient,
            "/api/v1/patients",
            new PatientRegistrationRequest
            {
                FirstName = "DEMO-Eşzamanlı-B",
                LastName = "Hasta",
                DateOfBirth = new DateOnly(1991, 2, 2),
                Gender = "Male",
                Email = "DEMO-concurrent-b@hospital.invalid",
            });

        var registrationResponses = await Task.WhenAll(firstRegistrationTask, secondRegistrationTask);
        Assert.All(registrationResponses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        var firstPatient = await registrationResponses[0].Content.ReadFromJsonAsync<PatientDetailResponse>();
        var secondPatient = await registrationResponses[1].Content.ReadFromJsonAsync<PatientDetailResponse>();
        Assert.NotNull(firstPatient);
        Assert.NotNull(secondPatient);
        Assert.NotEqual(firstPatient.MedicalRecordNumber, secondPatient.MedicalRecordNumber);

        // 6. Denetim günlüğünde arama işlemlerinin yer aldığını doğrula
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var auditLogs = await auditDb.AuditLogs.ToListAsync();
            Assert.Contains(auditLogs, l => l.Action == AuditAction.PatientSearch);
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
}
