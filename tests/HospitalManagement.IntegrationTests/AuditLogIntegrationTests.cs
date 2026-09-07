using System.Net;
using System.Net.Http.Json;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Domain;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class AuditLogIntegrationTests
{
    private const string AdminEmail = "DEMO-admin-audit@hospital.invalid";
    private const string AdminPassword = "DEMO-Admin-Pass!1";

    private const string DoctorEmail = "DEMO-doc-audit@hospital.invalid";
    private const string DoctorPassword = "DEMO-Doc-Pass!1";

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F02-G06")]
    public async Task AuditLogsArePersistedTamperCheckedAndProtectedByPermission()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        // Run EF Core migrations for both IdentityAccess and AuditPrivacy
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identityDb = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
            await identityDb.Database.MigrateAsync();

            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            await auditDb.Database.MigrateAsync();
        }

        // Seed users: Admin (has AuditTechnicalView) and Doctor (does NOT have AuditTechnicalView)
        var adminPersonId = Guid.NewGuid();
        var docPersonId = Guid.NewGuid();

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var adminUser = ApplicationUser.CreateInvitedStaff(
                Guid.NewGuid(),
                adminPersonId,
                AdminEmail,
                DateTime.UtcNow);
            adminUser.AcceptStaffInvitation();
            var createAdmin = await userManager.CreateAsync(adminUser, AdminPassword);
            Assert.True(createAdmin.Succeeded);
            await userManager.AddToRoleAsync(adminUser, HospitalRoles.SystemAdministrator);

            var docUser = ApplicationUser.CreateInvitedStaff(
                Guid.NewGuid(),
                docPersonId,
                DoctorEmail,
                DateTime.UtcNow);
            docUser.AcceptStaffInvitation();
            var createDoc = await userManager.CreateAsync(docUser, DoctorPassword);
            Assert.True(createDoc.Succeeded);
            await userManager.AddToRoleAsync(docUser, HospitalRoles.Doctor);
        }

        // 1. Doctor logs in (generates audit event: UserLogin)
        using var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, DoctorEmail, DoctorPassword);
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // 2. Doctor attempts to view audit logs -> 403 Forbidden (Doctor does not have AuditTechnicalView)
        var forbiddenResponse = await doctorClient.GetAsync("/api/v1/audit/logs");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        // 3. Admin logs in (generates audit event: UserLogin)
        using var adminClient = CreateSecureClient(application);
        var adminLogin = await LoginAsync(adminClient, AdminEmail, AdminPassword);
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        // 4. Admin queries audit logs -> 200 OK
        var auditLogsResponse = await adminClient.GetAsync("/api/v1/audit/logs");
        Assert.Equal(HttpStatusCode.OK, auditLogsResponse.StatusCode);

        var auditLogs = await auditLogsResponse.Content.ReadFromJsonAsync<IReadOnlyList<AuditEvent>>();
        Assert.NotNull(auditLogs);
        Assert.NotEmpty(auditLogs);

        // Verify login audit events exist for Doctor
        var docLoginLog = auditLogs.FirstOrDefault(l => l.Action == AuditAction.UserLogin && l.ActorPersonId == docPersonId);
        Assert.NotNull(docLoginLog);
        Assert.Equal(AuditOutcome.Success, docLoginLog.Outcome);

        // 5. Admin checks tamper integrity for the log -> 200 OK, isTamperFree == true
        var integrityResponse = await adminClient.GetAsync($"/api/v1/audit/integrity-check/{docLoginLog.Id}");
        Assert.Equal(HttpStatusCode.OK, integrityResponse.StatusCode);

        var integrityResult = await integrityResponse.Content.ReadFromJsonAsync<AuditIntegrityResultDto>();
        Assert.NotNull(integrityResult);
        Assert.True(integrityResult.IsTamperFree);

        // 6. Direct update attempt on audit logs in AuditPrivacyDbContext throws InvalidOperationException (Append-only)
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var entry = await auditDb.AuditLogs.FirstAsync(l => l.Id == docLoginLog.Id);
            auditDb.Entry(entry).State = EntityState.Modified;
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => auditDb.SaveChangesAsync());
            Assert.Contains("append-only", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed record AuditIntegrityResultDto(Guid Id, bool IsTamperFree);

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

