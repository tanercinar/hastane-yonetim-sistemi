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

public sealed class AdminUserManagementIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F02-G08")]
    public async Task AdminUserManagementFlowEnforcesLastAdminProtectionAndGeneratesAuditLogs()
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

        // Seed demo data
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await seeder.SeedAsync();
        }

        using var client = CreateSecureClient(application);

        var adminDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.SystemAdministrator);
        var docDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Doctor);

        // 1. Admin girişi yap
        var adminLoginResponse = await LoginAsync(client, adminDef.Email, adminDef.Password);
        Assert.Equal(HttpStatusCode.OK, adminLoginResponse.StatusCode);

        // 2. Kullanıcıları listele
        var usersResponse = await client.GetAsync("/api/v1/identity/users");
        Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);
        var users = await usersResponse.Content.ReadFromJsonAsync<UserListResponse>();
        Assert.NotNull(users);
        Assert.True(users.TotalCount >= 10);
        Assert.Contains(users.Items, u => u.Email == adminDef.Email);
        Assert.Contains(users.Items, u => u.Email == docDef.Email);

        // 3. Doktorun durumunu değiştir (Etkin -> Devre Dışı)
        var disableDocResponse = await PostWithAntiforgeryAsync(
            client,
            $"/api/v1/identity/users/{docDef.UserId}/status",
            new UpdateUserStatusRequest { IsEnabled = false });
        Assert.Equal(HttpStatusCode.OK, disableDocResponse.StatusCode);

        // 4. Admin kendi hesabını devre dışı bırakmayı denesin -> Son admin engeli (400 Bad Request)
        var disableSelfResponse = await PostWithAntiforgeryAsync(
            client,
            $"/api/v1/identity/users/{adminDef.UserId}/status",
            new UpdateUserStatusRequest { IsEnabled = false });
        Assert.Equal(HttpStatusCode.BadRequest, disableSelfResponse.StatusCode);

        // 5. Admin kendi ADM rolünü kaldırmayı denesin -> Son admin engeli (400 Bad Request)
        var removeSelfAdminRoleResponse = await PostWithAntiforgeryAsync(
            client,
            $"/api/v1/identity/users/{adminDef.UserId}/roles",
            new UpdateUserRolesRequest { Roles = [HospitalRoles.Doctor] });
        Assert.Equal(HttpStatusCode.BadRequest, removeSelfAdminRoleResponse.StatusCode);

        // 6. Doktorun rollerini güncelle
        var updateDocRolesResponse = await PostWithAntiforgeryAsync(
            client,
            $"/api/v1/identity/users/{docDef.UserId}/roles",
            new UpdateUserRolesRequest { Roles = [HospitalRoles.Doctor, HospitalRoles.Nurse] });
        Assert.Equal(HttpStatusCode.OK, updateDocRolesResponse.StatusCode);

        // Doktoru tekrar etkinleştir
        await PostWithAntiforgeryAsync(
            client,
            $"/api/v1/identity/users/{docDef.UserId}/status",
            new UpdateUserStatusRequest { IsEnabled = true });

        await LogoutAsync(client);

        // 7. Doktor girişi yap ve yetkisiz erişimleri sına
        var docLoginResponse = await LoginAsync(client, docDef.Email, docDef.Password);
        Assert.Equal(HttpStatusCode.OK, docLoginResponse.StatusCode);

        var forbiddenUsersResponse = await client.GetAsync("/api/v1/identity/users");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenUsersResponse.StatusCode);

        var forbiddenStatusResponse = await PostWithAntiforgeryAsync(
            client,
            $"/api/v1/identity/users/{adminDef.UserId}/status",
            new UpdateUserStatusRequest { IsEnabled = false });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenStatusResponse.StatusCode);

        var forbiddenRolesResponse = await PostWithAntiforgeryAsync(
            client,
            $"/api/v1/identity/users/{adminDef.UserId}/roles",
            new UpdateUserRolesRequest { Roles = [HospitalRoles.Doctor] });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenRolesResponse.StatusCode);

        await LogoutAsync(client);

        // 8. Audit loglarını veritabanından doğrula
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var logs = await auditDb.AuditLogs.ToListAsync();

            Assert.Contains(logs, l => l.Action == AuditAction.UserDisable && l.TargetResourceId == docDef.UserId.ToString());
            Assert.Contains(logs, l => l.Action == AuditAction.RoleAssign && l.TargetResourceId == docDef.UserId.ToString());
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
