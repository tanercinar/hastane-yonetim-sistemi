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

public sealed class PatientIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F03-G01")]
    public async Task PatientMasterIndexLifecycleDuplicateCheckConcurrencyAndAccessControl()
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
        var docDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Doctor);
        var patDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Patient);

        // =========================================================================
        // 1. KAYIT PERSONELİ AKIŞI: DUPLICATE KONTROLÜ VE YENİ HASTA KAYDI
        // =========================================================================
        var regLogin = await LoginAsync(client, regDef.Email, regDef.Password);
        Assert.Equal(HttpStatusCode.OK, regLogin.StatusCode);

        // Mükerrer kontrolü: Var olan "Ayşe Yılmaz" / "99900000001" için uyarı beklenir
        var dupResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/patients/duplicate-check",
            new DuplicatePatientCheckRequest
            {
                FirstName = "Ayşe",
                LastName = "Yılmaz",
                DateOfBirth = new DateOnly(1985, 4, 15),
                NationalIdSynthetic = "99900000001",
            });
        Assert.Equal(HttpStatusCode.OK, dupResponse.StatusCode);
        var dupResult = await dupResponse.Content.ReadFromJsonAsync<DuplicateCheckResponse>();
        Assert.NotNull(dupResult);
        Assert.True(dupResult.HasPotentialDuplicate);
        Assert.NotEmpty(dupResult.Candidates);

        // Yeni hasta kaydı oluştur
        var newPersonId = Guid.NewGuid();
        var regRequest = new PatientRegistrationRequest
        {
            PersonId = newPersonId,
            FirstName = "Kemal",
            LastName = "Öztürk",
            DateOfBirth = new DateOnly(1978, 11, 25),
            Gender = "Male",
            NationalIdSynthetic = "99900000099",
            PhoneNumber = "+90 555 777 8899",
            Email = "DEMO-kemal@hospital.invalid",
            Address = new Contracts.Patients.AddressDto("Bursa", "Nilüfer", "FSM Bulvarı No: 20", "16130"),
            EmergencyContact = new Contracts.Patients.EmergencyContactDto("Zeynep Öztürk", "Eşi", "+90 555 777 8800"),
            CommunicationPreferences = new Contracts.Patients.CommunicationPreferencesDto(true, true, "tr"),
        };

        var createResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/patients",
            regRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientDetailResponse>();
        Assert.NotNull(createdPatient);
        Assert.StartsWith("MRN-", createdPatient.MedicalRecordNumber);
        Assert.Equal("Kemal", createdPatient.FirstName);
        Assert.Equal(1, createdPatient.Version);

        // Concurrency: Başarılı güncelleme (Version 1 -> 2)
        var updateRequest = new PatientUpdateRequest
        {
            FirstName = "Kemal",
            LastName = "Öztürk",
            DateOfBirth = new DateOnly(1978, 11, 25),
            Gender = "Male",
            NationalIdSynthetic = "99900000099",
            PhoneNumber = "+90 555 777 8899",
            Email = "DEMO-kemal@hospital.invalid",
            Address = new Contracts.Patients.AddressDto("Bursa", "Nilüfer", "FSM Bulvarı No: 22", "16130"), // Güncellenmiş adres
            EmergencyContact = new Contracts.Patients.EmergencyContactDto("Zeynep Öztürk", "Eşi", "+90 555 777 8800"),
            CommunicationPreferences = new Contracts.Patients.CommunicationPreferencesDto(true, true, "tr"),
        };

        var updateResponse = await PutWithAntiforgeryAsync(
            client,
            $"/api/v1/patients/{createdPatient.Id}",
            updateRequest,
            ifMatchVersion: createdPatient.Version);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedPatient = await updateResponse.Content.ReadFromJsonAsync<PatientDetailResponse>();
        Assert.NotNull(updatedPatient);
        Assert.Equal(2, updatedPatient.Version);

        // Concurrency: Eski versiyonla (Version 1) tekrar güncellemeyi deneme -> 409 Conflict
        var staleUpdateResponse = await PutWithAntiforgeryAsync(
            client,
            $"/api/v1/patients/{createdPatient.Id}",
            updateRequest,
            ifMatchVersion: 1);
        Assert.Equal(HttpStatusCode.Conflict, staleUpdateResponse.StatusCode);

        await LogoutAsync(client);

        // =========================================================================
        // 2. DOKTOR AKIŞI: HASTA ARAMA VE DETAY GÖRÜNTÜLEME
        // =========================================================================
        var docLogin = await LoginAsync(client, docDef.Email, docDef.Password);
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var searchResponse = await client.GetAsync("/api/v1/patients?query=Öztürk");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var searchList = await searchResponse.Content.ReadFromJsonAsync<PatientListResponse>();
        Assert.NotNull(searchList);
        Assert.Contains(searchList.Items, p => p.LastName == "Öztürk");

        var docViewResponse = await client.GetAsync($"/api/v1/patients/{createdPatient.Id}");
        Assert.Equal(HttpStatusCode.OK, docViewResponse.StatusCode);

        await LogoutAsync(client);

        // =========================================================================
        // 3. HASTA AKIŞI VE IDOR KORUMASI: KENDİ PROFİLİNE ERİŞİM & BAŞKASINA YASAK
        // =========================================================================
        var patLogin = await LoginAsync(client, patDef.Email, patDef.Password);
        Assert.Equal(HttpStatusCode.OK, patLogin.StatusCode);

        var patSeedDef = PatientDataSeeder.DemoPatients.First(p => p.Email == patDef.Email);

        // Hasta kendi person ID'si ile profilini görebilir
        var ownRecordResponse = await client.GetAsync($"/api/v1/patients/by-person/{patSeedDef.PersonId}");
        Assert.Equal(HttpStatusCode.OK, ownRecordResponse.StatusCode);

        // Hasta başka bir hastanın (Kemal Öztürk) detayına doğrudan ID ile ERİŞEMEZ -> 403 Forbidden
        var idorAttemptResponse = await client.GetAsync($"/api/v1/patients/{createdPatient.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, idorAttemptResponse.StatusCode);

        await LogoutAsync(client);

        // =========================================================================
        // 4. DENETİM İZİ DOĞRULAMASI
        // =========================================================================
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var logs = await auditDb.AuditLogs.ToListAsync();

            Assert.Contains(logs, l => l.Action == AuditAction.PatientRegister && l.TargetResourceId == createdPatient.Id.ToString());
            Assert.Contains(logs, l => l.Action == AuditAction.PatientDemographicsEdit && l.TargetResourceId == createdPatient.Id.ToString());
            Assert.Contains(logs, l => l.Action == AuditAction.PatientView && l.TargetResourceId == createdPatient.Id.ToString());
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

    private static Task<HttpResponseMessage> LogoutAsync(HttpClient client) =>
        PostWithAntiforgeryAsync<object?>(
            client,
            "/api/v1/identity/sessions/logout",
            null);

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
        TRequest body,
        long? ifMatchVersion = null)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/v1/identity/antiforgery");
        Assert.NotNull(token);
        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        request.Headers.Add("X-HMS-CSRF", token.Token);
        if (ifMatchVersion.HasValue)
        {
            request.Headers.Add("If-Match", $"\"{ifMatchVersion.Value}\"");
        }

        return await client.SendAsync(request);
    }
}
