using System.Net;
using System.Net.Http.Json;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class Phase2SecurityGateIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F02-KAPI")]
    public async Task Phase2SecurityGateFullRoleAndPermissionMatrixValidation()
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
        }

        // Seed deterministic demo data
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await seeder.SeedAsync();
        }

        using var client = CreateSecureClient(application);

        // =========================================================================
        // 1. ANONYMOUS ACTOR SINAILARI (Varsayılan Ret & Kimlik Doğrulama Zorunluluğu)
        // =========================================================================
        var anonSessionResponse = await client.GetAsync("/api/v1/identity/session");
        Assert.Equal(HttpStatusCode.Unauthorized, anonSessionResponse.StatusCode);

        var anonUsersResponse = await client.GetAsync("/api/v1/identity/users");
        Assert.Equal(HttpStatusCode.Unauthorized, anonUsersResponse.StatusCode);

        var anonProfileProbe = await client.GetAsync("/api/v1/platform/auth-probes/profile-view");
        Assert.Equal(HttpStatusCode.Unauthorized, anonProfileProbe.StatusCode);

        var anonClinicalProbe = await client.GetAsync("/api/v1/platform/auth-probes/clinical-note-sign");
        Assert.Equal(HttpStatusCode.Unauthorized, anonClinicalProbe.StatusCode);

        var anonAuditResponse = await client.GetAsync("/api/v1/audit/logs");
        Assert.Equal(HttpStatusCode.Unauthorized, anonAuditResponse.StatusCode);

        // =========================================================================
        // 2. CSRF & ÇEREZ GÜVENLİK KONTROLLERİ
        // =========================================================================
        // CSRF header'ı olmadan POST isteği reddedilir (400 Bad Request)
        using var noCsrfRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/sessions")
        {
            Content = JsonContent.Create(new LoginRequest
            {
                Email = "DEMO-admin@hospital.invalid",
                Password = "DEMO-Admin-Pass!1",
            }),
        };
        var noCsrfResponse = await client.SendAsync(noCsrfRequest);
        Assert.Equal(HttpStatusCode.BadRequest, noCsrfResponse.StatusCode);

        // =========================================================================
        // 3. HASTA (PATIENT) ROLÜ VE SINIRLARI
        // =========================================================================
        var patientDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Patient);
        var patientLogin = await LoginAsync(client, patientDef.Email, patientDef.Password);
        Assert.Equal(HttpStatusCode.OK, patientLogin.StatusCode);

        // Hasta kendi oturumunu görüntüleyebilir
        var patSession = await client.GetFromJsonAsync<CurrentAccountResponse>("/api/v1/identity/session");
        Assert.NotNull(patSession);
        Assert.Equal(patientDef.Email, patSession.Email);
        Assert.Equal("Patient", patSession.AccountKind);

        // Hasta profil görüntüleme iznine sahiptir
        var patProfileProbe = await client.GetAsync("/api/v1/platform/auth-probes/profile-view");
        Assert.Equal(HttpStatusCode.OK, patProfileProbe.StatusCode);

        // Hasta yönetim uç noktasına ERİŞEMEZ (403 Forbidden)
        var patUsersProbe = await client.GetAsync("/api/v1/identity/users");
        Assert.Equal(HttpStatusCode.Forbidden, patUsersProbe.StatusCode);

        // Hasta organizasyon yönetimine ERİŞEMEZ (403 Forbidden)
        var patOrgProbe = await client.GetAsync("/api/v1/platform/auth-probes/organization-manage");
        Assert.Equal(HttpStatusCode.Forbidden, patOrgProbe.StatusCode);

        // Hasta klinik not imzalama yetkisine ERİŞEMEZ (403 Forbidden)
        var patClinicalProbe = await client.GetAsync("/api/v1/platform/auth-probes/clinical-note-sign");
        Assert.Equal(HttpStatusCode.Forbidden, patClinicalProbe.StatusCode);

        // Hasta audit loglarını göremez (403 Forbidden)
        var patAuditProbe = await client.GetAsync("/api/v1/audit/logs");
        Assert.Equal(HttpStatusCode.Forbidden, patAuditProbe.StatusCode);

        await LogoutAsync(client);

        // =========================================================================
        // 4. DOKTOR (DOC) VE HEMŞİRE (NUR) ROLÜ VE KLİNİK SINIRLARI
        // =========================================================================
        var docDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Doctor);
        var docLogin = await LoginAsync(client, docDef.Email, docDef.Password);
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var docSession = await client.GetFromJsonAsync<CurrentAccountResponse>("/api/v1/identity/session");
        Assert.NotNull(docSession);
        Assert.Equal("Staff", docSession.AccountKind);

        // Doktor klinik not imzalayabilir (200 OK)
        var docClinicalProbe = await client.GetAsync("/api/v1/platform/auth-probes/clinical-note-sign");
        Assert.Equal(HttpStatusCode.OK, docClinicalProbe.StatusCode);

        // Doktor kullanıcı yönetimine ERİŞEMEZ (403 Forbidden)
        var docUsersProbe = await client.GetAsync("/api/v1/identity/users");
        Assert.Equal(HttpStatusCode.Forbidden, docUsersProbe.StatusCode);

        // Doktor audit teknik loglarına ERİŞEMEZ (403 Forbidden)
        var docAuditProbe = await client.GetAsync("/api/v1/audit/logs");
        Assert.Equal(HttpStatusCode.Forbidden, docAuditProbe.StatusCode);

        await LogoutAsync(client);

        // =========================================================================
        // 5. YARDIMCI SAĞLIK ROLLERİ (ECZACI, LABORANT, RADYOLOJİ, DANIŞMA)
        // =========================================================================
        var alliedRoles = new[]
        {
            IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Pharmacist),
            IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.LaboratoryStaff),
            IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.RadiologyStaff),
            IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.RegistrationStaff),
        };

        foreach (var alliedUser in alliedRoles)
        {
            var loginResult = await LoginAsync(client, alliedUser.Email, alliedUser.Password);
            Assert.Equal(HttpStatusCode.OK, loginResult.StatusCode);

            // Kullanıcı yönetimine erişemez
            var usersDenied = await client.GetAsync("/api/v1/identity/users");
            Assert.Equal(HttpStatusCode.Forbidden, usersDenied.StatusCode);

            // Doğrudan hekim klinik not imzasına erişemez
            var signDenied = await client.GetAsync("/api/v1/platform/auth-probes/clinical-note-sign");
            Assert.Equal(HttpStatusCode.Forbidden, signDenied.StatusCode);

            await LogoutAsync(client);
        }

        // =========================================================================
        // 6. SİSTEM YÖNETİCİSİ (SYSTEM ADMINISTRATOR - ADM) VE KLİNİK VERİ YASAĞI
        // =========================================================================
        var adminDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.SystemAdministrator);
        var adminLogin = await LoginAsync(client, adminDef.Email, adminDef.Password);
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        // Admin kullanıcıları yönetebilir (200 OK)
        var adminUsers = await client.GetAsync("/api/v1/identity/users");
        Assert.Equal(HttpStatusCode.OK, adminUsers.StatusCode);

        // Admin organizasyonu yönetebilir (200 OK)
        var adminOrg = await client.GetAsync("/api/v1/platform/auth-probes/organization-manage");
        Assert.Equal(HttpStatusCode.OK, adminOrg.StatusCode);

        // Admin KLİNİK İÇERİĞE/NOT İMZALAMAYA ERİŞEMEZ (403 Forbidden - ADR-0002 / ADR-0004)
        var adminClinicalDenied = await client.GetAsync("/api/v1/platform/auth-probes/clinical-note-sign");
        Assert.Equal(HttpStatusCode.Forbidden, adminClinicalDenied.StatusCode);

        await LogoutAsync(client);

        // =========================================================================
        // 7. DENETİM İZİ VE TAHRİFAT BÜTÜNLÜĞÜ DOĞRULAMASI
        // =========================================================================
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var logs = await auditDb.AuditLogs.OrderBy(l => l.CreatedAtUtc).ToListAsync();

            Assert.NotEmpty(logs);
            foreach (var log in logs)
            {
                Assert.NotEmpty(log.RecordHash);
                Assert.True(log.VerifyHashIntegrity(), $"Denetim kaydı ID={log.Id} hash doğrulaması başarısız!");
            }
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
}
