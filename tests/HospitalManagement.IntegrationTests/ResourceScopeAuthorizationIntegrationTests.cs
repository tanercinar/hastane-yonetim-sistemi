using System.Net;
using System.Net.Http.Json;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Host.Api;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Domain;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Domain;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class ResourceScopeAuthorizationIntegrationTests
{
    private const string PatientAEmail = "DEMO-patient-a@hospital.invalid";
    private const string PatientBEmail = "DEMO-patient-b@hospital.invalid";
    private const string PatientPassword = "DEMO-Patient-Pass!1";

    private const string DoctorEmail = "DEMO-doctor-scope@hospital.invalid";
    private const string DoctorPassword = "DEMO-Doctor-Pass!1";

    private const string NurseEmail = "DEMO-nurse-scope@hospital.invalid";
    private const string NursePassword = "DEMO-Nurse-Pass!1";

    private const string AdminEmail = "DEMO-admin-scope@hospital.invalid";
    private const string AdminPassword = "DEMO-Admin-Pass!1";

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F02-G04")]
    public async Task HorizontalIdorAndResourceScopesAreEnforcedAgainstAttacks()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        // 1. Veritabanı migration'larını çalıştır
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identityDb = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
            await identityDb.Database.MigrateAsync();

            var orgDb = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
            await orgDb.Database.MigrateAsync();
        }

        using var client = CreateSecureClient(application);

        // 2. Hasta A ve Hasta B oluştur
        var patientAPersonId = await RegisterAndConfirmPatientAsync(client, messages, PatientAEmail, PatientPassword);
        var patientBPersonId = await RegisterAndConfirmPatientAsync(client, messages, PatientBEmail, PatientPassword);

        // 3. Test 1: Yatay IDOR (Hasta A -> Hasta B kaydına erişim girişimi)
        // Hasta A ile giriş yap
        var patientALogin = await LoginAsync(client, PatientAEmail, PatientPassword);
        Assert.Equal(HttpStatusCode.OK, patientALogin.StatusCode);

        // Hasta A kendi kaydına erişebilir (200 OK)
        var ownRecordResponse = await client.GetAsync($"/api/v1/platform/resource-probes/patient-record/{patientAPersonId}");
        Assert.Equal(HttpStatusCode.OK, ownRecordResponse.StatusCode);

        // Hasta A, Hasta B'nin kaydına erişmeye çalıştığında sistem isteği 403 Forbidden ile reddeder (Yatay IDOR Engellendi!)
        var idorAttackResponse = await client.GetAsync($"/api/v1/platform/resource-probes/patient-record/{patientBPersonId}");
        Assert.Equal(HttpStatusCode.Forbidden, idorAttackResponse.StatusCode);

        // 4. Test 2: Bakım İlişkisi & Klinik Kapsam (Doktor -> Hasta A vs Hasta B)
        var doctorPersonId = Guid.NewGuid();
        await CreateStaffUserAsync(application, DoctorEmail, DoctorPassword, doctorPersonId, HospitalRoles.Doctor);

        using var doctorClient = CreateSecureClient(application);
        var doctorLogin = await LoginAsync(doctorClient, DoctorEmail, DoctorPassword);
        Assert.Equal(HttpStatusCode.OK, doctorLogin.StatusCode);

        // Doktor henüz Hasta A veya Hasta B ile bakım ilişkisine sahip değil -> İkisi de 403 Forbidden
        var docWithoutRelA = await doctorClient.GetAsync($"/api/v1/platform/resource-probes/clinical-encounter/{patientAPersonId}");
        Assert.Equal(HttpStatusCode.Forbidden, docWithoutRelA.StatusCode);

        var docWithoutRelB = await doctorClient.GetAsync($"/api/v1/platform/resource-probes/clinical-encounter/{patientBPersonId}");
        Assert.Equal(HttpStatusCode.Forbidden, docWithoutRelB.StatusCode);

        // Doktor ile Hasta A arasında bakım ilişkisi kurulur
        var establishRelResponse = await doctorClient.PostAsJsonAsync(
            "/api/v1/platform/resource-probes/care-relationships",
            new ApiEndpointRouteBuilderExtensions.EstablishCareRelationshipRequest(doctorPersonId, patientAPersonId));
        Assert.Equal(HttpStatusCode.OK, establishRelResponse.StatusCode);

        // Doktor artık bakım ilişkisi olan Hasta A'nın klinik kaydına erişebilir (200 OK)
        var docWithRelA = await doctorClient.GetAsync($"/api/v1/platform/resource-probes/clinical-encounter/{patientAPersonId}");
        Assert.Equal(HttpStatusCode.OK, docWithRelA.StatusCode);

        // Doktor bakım ilişkisi olmayan Hasta B'nin klinik kaydına hâlâ erişemez (403 Forbidden)
        var docStillDeniedB = await doctorClient.GetAsync($"/api/v1/platform/resource-probes/clinical-encounter/{patientBPersonId}");
        Assert.Equal(HttpStatusCode.Forbidden, docStillDeniedB.StatusCode);

        // 5. Test 3: Bölüm Kapsamı (Hemşire -> Atandığı Bölüm vs Diğer Bölüm)
        var nursePersonId = Guid.NewGuid();
        await CreateStaffUserAsync(application, NurseEmail, NursePassword, nursePersonId, HospitalRoles.Nurse);

        Guid department1Id;
        Guid department2Id;
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var orgDb = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
            var departments = await orgDb.Departments.AsNoTracking().Take(2).ToListAsync();
            Assert.True(departments.Count >= 2, "En az 2 demo bölüm bulunmalı.");
            department1Id = departments[0].Id;
            department2Id = departments[1].Id;

            // Hemşireye StaffProfile ve Department 1 ataması yap
            var staffProfile = StaffProfile.Create(
                Guid.NewGuid(),
                departments[0].HospitalId,
                nursePersonId,
                "DEMO-NURSE-001",
                ClinicalProfession.Nurse,
                null,
                DateTime.UtcNow);
            orgDb.StaffProfiles.Add(staffProfile);

            var assignment = StaffDepartmentAssignment.Create(
                Guid.NewGuid(),
                departments[0].HospitalId,
                staffProfile.Id,
                department1Id,
                isPrimary: true,
                startsAtUtc: DateTime.UtcNow.AddDays(-1));
            orgDb.StaffDepartmentAssignments.Add(assignment);

            await orgDb.SaveChangesAsync();
        }

        using var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, NurseEmail, NursePassword);
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        // Hemşire atandığı Department 1 kaynağına erişebilir (200 OK)
        var nurseDept1Response = await nurseClient.PostAsync(
            $"/api/v1/platform/resource-probes/department-record/{department1Id}",
            null);
        Assert.Equal(HttpStatusCode.OK, nurseDept1Response.StatusCode);

        // Hemşire atanmadığı Department 2 kaynağına erişemez (403 Forbidden)
        var nurseDept2Response = await nurseClient.PostAsync(
            $"/api/v1/platform/resource-probes/department-record/{department2Id}",
            null);
        Assert.Equal(HttpStatusCode.Forbidden, nurseDept2Response.StatusCode);

        // 6. Test 4: Sistem Yöneticisi (ADM) Klinik Veri Erişim Yasağı
        var adminPersonId = Guid.NewGuid();
        await CreateStaffUserAsync(application, AdminEmail, AdminPassword, adminPersonId, HospitalRoles.SystemAdministrator);

        using var adminClient = CreateSecureClient(application);
        var adminLogin = await LoginAsync(adminClient, AdminEmail, AdminPassword);
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        // Admin klinik encounter kaydına erişmeye çalıştığında 403 Forbidden alır
        var adminClinicalResponse = await adminClient.GetAsync($"/api/v1/platform/resource-probes/clinical-encounter/{patientAPersonId}");
        Assert.Equal(HttpStatusCode.Forbidden, adminClinicalResponse.StatusCode);
    }

    private static HttpClient CreateSecureClient(ApiWebApplicationFactory application) =>
        application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost", UriKind.Absolute),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    private static async Task<Guid> RegisterAndConfirmPatientAsync(
        HttpClient client,
        InMemoryIdentityMessageSender messages,
        string email,
        string password)
    {
        using var registration = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/patient-registrations",
            new RegisterPatientRequest
            {
                Email = email,
                Password = password,
            });
        Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);
        var code = messages.Latest(
            IdentityMessageKind.PatientEmailConfirmation,
            email).ActionCode;
        using var confirmation = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/patient-email-confirmations",
            new ConfirmPatientEmailRequest
            {
                Email = email,
                Code = code,
            });
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);

        using var login = await LoginAsync(client, email, password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var session = await client.GetFromJsonAsync<CurrentAccountResponse>("/api/v1/identity/session");
        Assert.NotNull(session);
        return Guid.Parse(session.PersonId);
    }

    private static async Task CreateStaffUserAsync(
        ApiWebApplicationFactory application,
        string email,
        string password,
        Guid personId,
        string role)
    {
        await using var scope = application.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var staffUser = ApplicationUser.CreateInvitedStaff(
            Guid.NewGuid(),
            personId,
            email,
            DateTime.UtcNow);
        staffUser.AcceptStaffInvitation();

        var createResult = await userManager.CreateAsync(staffUser, password);
        Assert.True(createResult.Succeeded);

        var addRoleResult = await userManager.AddToRoleAsync(staffUser, role);
        Assert.True(addRoleResult.Succeeded);
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

