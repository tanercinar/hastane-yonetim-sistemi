using System.Net;
using System.Net.Http.Json;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class DemoDataAndSecurityIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F02-G07")]
    public async Task SeededDemoUsersCanAuthenticateAndHaveCorrectRolesInPostgreSql()
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
        }

        // Run seed twice to test live PostgreSQL idempotency
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await seeder.SeedAsync();
            await seeder.SeedAsync();
        }

        using var client = CreateSecureClient(application);

        // Test login for Admin
        var adminDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.SystemAdministrator);
        var adminLoginResponse = await LoginAsync(client, adminDef.Email, adminDef.Password);
        Assert.Equal(HttpStatusCode.OK, adminLoginResponse.StatusCode);

        var adminSessionResponse = await client.GetAsync("/api/v1/identity/session");
        Assert.Equal(HttpStatusCode.OK, adminSessionResponse.StatusCode);
        var adminSession = await adminSessionResponse.Content.ReadFromJsonAsync<CurrentAccountResponse>();
        Assert.NotNull(adminSession);
        Assert.Equal(adminDef.Email, adminSession.Email);
        Assert.Equal("Staff", adminSession.AccountKind);

        // Admin seed endpoint'ine erişebilir (RoleAssign izni vardır)
        var adminSeedProbe = await PostWithAntiforgeryAsync<object?>(client, "/api/v1/identity/seed", null);
        Assert.Equal(HttpStatusCode.OK, adminSeedProbe.StatusCode);

        await LogoutAsync(client);

        // Test login for Doctor
        var docDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Doctor);
        var docLoginResponse = await LoginAsync(client, docDef.Email, docDef.Password);
        Assert.Equal(HttpStatusCode.OK, docLoginResponse.StatusCode);

        var docSessionResponse = await client.GetAsync("/api/v1/identity/session");
        Assert.Equal(HttpStatusCode.OK, docSessionResponse.StatusCode);
        var docSession = await docSessionResponse.Content.ReadFromJsonAsync<CurrentAccountResponse>();
        Assert.NotNull(docSession);
        Assert.Equal(docDef.Email, docSession.Email);
        Assert.Equal("Staff", docSession.AccountKind);

        // Doktor seed endpoint'ine erişemez (403 Forbidden)
        var docForbiddenProbe = await PostWithAntiforgeryAsync<object?>(client, "/api/v1/identity/seed", null);
        Assert.Equal(HttpStatusCode.Forbidden, docForbiddenProbe.StatusCode);

        await LogoutAsync(client);

        // Test login for Patient
        var patDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Patient);
        var patLoginResponse = await LoginAsync(client, patDef.Email, patDef.Password);
        Assert.Equal(HttpStatusCode.OK, patLoginResponse.StatusCode);

        var patSessionResponse = await client.GetAsync("/api/v1/identity/session");
        Assert.Equal(HttpStatusCode.OK, patSessionResponse.StatusCode);
        var patSession = await patSessionResponse.Content.ReadFromJsonAsync<CurrentAccountResponse>();
        Assert.NotNull(patSession);
        Assert.Equal(patDef.Email, patSession.Email);
        Assert.Equal("Patient", patSession.AccountKind);
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

