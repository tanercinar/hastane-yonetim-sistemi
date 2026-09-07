using System.Net;
using System.Net.Http.Json;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Domain;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class AuthorizationPolicyTests
{
    private const string PatientEmail = "DEMO-patient-auth@hospital.invalid";
    private const string PatientPassword = "DEMO-Patient-Password!1";
    private const string AdminEmail = "DEMO-admin-auth@hospital.invalid";
    private const string AdminPassword = "DEMO-Admin-Password!1";
    private const string DoctorEmail = "DEMO-doctor-auth@hospital.invalid";
    private const string DoctorPassword = "DEMO-Doctor-Password!1";

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F02-G03")]
    public async Task RoleSeedingAndPermissionAuthorizationPoliciesEnforceDefaultDeny()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        // 1. Migration doğrulama: 12 rolün identity_access.roles tablosuna eklendiğini doğrula
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
            await dbContext.Database.MigrateAsync();

            var roles = await dbContext.Roles.AsNoTracking().ToListAsync();
            Assert.Equal(12, roles.Count);

            foreach (var role in HospitalRoles.All)
            {
                Assert.Contains(roles, r => r.Name == role.Code);
            }
        }

        using var client = CreateSecureClient(application);

        // 2. Anonim erişim: Korunan endpoint 401 Unauthorized döner
        var unauthenticatedSession = await client.GetAsync("/api/v1/identity/session");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticatedSession.StatusCode);

        // 3. Hasta kaydı ve oturum açma: Patient rolü ve identity.profile.view-own izni
        await CreateConfirmedPatientAsync(client, messages);

        var loginResponse = await LoginAsync(client, PatientEmail, PatientPassword);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // 4. İzinli endpoint: Hasta kendi profil/oturumunu görüntüleyebilir (200 OK)
        var authenticatedSession = await client.GetAsync("/api/v1/identity/session");
        Assert.Equal(HttpStatusCode.OK, authenticatedSession.StatusCode);
        var sessionBody = await authenticatedSession.Content.ReadFromJsonAsync<CurrentAccountResponse>();
        Assert.NotNull(sessionBody);
        Assert.Equal(PatientEmail, sessionBody.Email);
        Assert.Equal(AccountKind.Patient.ToString(), sessionBody.AccountKind);

        // 5. Auth-probe testleri (Hasta):
        // 5a. Hasta kendi izni olan profile-view endpoint'ine erişebilir (200 OK)
        var profileViewResponse = await client.GetAsync("/api/v1/platform/auth-probes/profile-view");
        Assert.Equal(HttpStatusCode.OK, profileViewResponse.StatusCode);

        // 5b. Hasta organization.manage gerektiren endpoint'e eriştiğinde 403 Forbidden alır
        var orgManageResponse = await client.GetAsync("/api/v1/platform/auth-probes/organization-manage");
        Assert.Equal(HttpStatusCode.Forbidden, orgManageResponse.StatusCode);

        // 5c. Hasta bilinmeyen bir izinle korunan endpoint'e eriştiğinde 403 Forbidden alır (Default Deny)
        var unknownPermResponse = await client.GetAsync("/api/v1/platform/auth-probes/unknown-permission");
        Assert.Equal(HttpStatusCode.Forbidden, unknownPermResponse.StatusCode);

        // 6. Admin kullanıcısı oluşturup izinlerini doğrula
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var adminUser = ApplicationUser.CreateInvitedStaff(
                Guid.NewGuid(),
                Guid.NewGuid(),
                AdminEmail,
                DateTime.UtcNow);
            adminUser.AcceptStaffInvitation();

            var createResult = await userManager.CreateAsync(adminUser, AdminPassword);
            Assert.True(createResult.Succeeded);

            var addRoleResult = await userManager.AddToRoleAsync(adminUser, HospitalRoles.SystemAdministrator);
            Assert.True(addRoleResult.Succeeded);
        }

        // Admin ile oturum aç
        using var adminClient = CreateSecureClient(application);
        var adminLogin = await LoginAsync(adminClient, AdminEmail, AdminPassword);
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        var adminSession = await adminClient.GetAsync("/api/v1/identity/session");
        Assert.Equal(HttpStatusCode.OK, adminSession.StatusCode);
        var adminSessionBody = await adminSession.Content.ReadFromJsonAsync<CurrentAccountResponse>();
        Assert.NotNull(adminSessionBody);
        Assert.Equal(AdminEmail, adminSessionBody.Email);
        Assert.Equal(AccountKind.Staff.ToString(), adminSessionBody.AccountKind);

        // Admin organization.manage endpoint'ine erişebilir (200 OK)
        var adminOrgManage = await adminClient.GetAsync("/api/v1/platform/auth-probes/organization-manage");
        Assert.Equal(HttpStatusCode.OK, adminOrgManage.StatusCode);

        // Admin klinik not imzalama endpoint'ine erişemez (403 Forbidden)
        var adminClinicalSign = await adminClient.GetAsync("/api/v1/platform/auth-probes/clinical-note-sign");
        Assert.Equal(HttpStatusCode.Forbidden, adminClinicalSign.StatusCode);

        // 7. Doktor kullanıcısı oluşturup izinlerini doğrula
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var doctorUser = ApplicationUser.CreateInvitedStaff(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DoctorEmail,
                DateTime.UtcNow);
            doctorUser.AcceptStaffInvitation();

            var createResult = await userManager.CreateAsync(doctorUser, DoctorPassword);
            Assert.True(createResult.Succeeded);

            var addRoleResult = await userManager.AddToRoleAsync(doctorUser, HospitalRoles.Doctor);
            Assert.True(addRoleResult.Succeeded);
        }

        // Doktor ile oturum aç
        using var doctorClient = CreateSecureClient(application);
        var doctorLogin = await LoginAsync(doctorClient, DoctorEmail, DoctorPassword);
        Assert.Equal(HttpStatusCode.OK, doctorLogin.StatusCode);

        // Doktor klinik not imzalama endpoint'ine erişebilir (200 OK)
        var doctorClinicalSign = await doctorClient.GetAsync("/api/v1/platform/auth-probes/clinical-note-sign");
        Assert.Equal(HttpStatusCode.OK, doctorClinicalSign.StatusCode);

        // Doktor organization.manage endpoint'ine erişemez (403 Forbidden)
        var doctorOrgManage = await doctorClient.GetAsync("/api/v1/platform/auth-probes/organization-manage");
        Assert.Equal(HttpStatusCode.Forbidden, doctorOrgManage.StatusCode);
    }

    private static HttpClient CreateSecureClient(ApiWebApplicationFactory application) =>
        application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost", UriKind.Absolute),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    private static async Task CreateConfirmedPatientAsync(
        HttpClient client,
        InMemoryIdentityMessageSender messages)
    {
        using var registration = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/patient-registrations",
            new RegisterPatientRequest
            {
                Email = PatientEmail,
                Password = PatientPassword,
            });
        Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);
        var code = messages.Latest(
            IdentityMessageKind.PatientEmailConfirmation,
            PatientEmail).ActionCode;
        using var confirmation = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/patient-email-confirmations",
            new ConfirmPatientEmailRequest
            {
                Email = PatientEmail,
                Code = code,
            });
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
    }

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

