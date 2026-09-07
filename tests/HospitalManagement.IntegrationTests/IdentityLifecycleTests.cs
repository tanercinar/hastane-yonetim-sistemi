using System.Net;
using System.Net.Http.Json;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Domain;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HospitalManagement.IntegrationTests;

public sealed class IdentityLifecycleTests
{
    private const string PatientEmail = "DEMO-patient-lifecycle@hospital.invalid";
    private const string PatientPassword = "DEMO-Patient-Password!1";
    private const string ReplacementPassword = "DEMO-Replacement-Password!2";

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F02-G02")]
    public async Task PatientRegistrationConfirmationSessionAndLogoutUseSecureContracts()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();
        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();
            Assert.Contains(pendingMigrations, m => m.EndsWith("_InitialIdentityAccess", StringComparison.Ordinal));
            Assert.Contains(pendingMigrations, m => m.EndsWith("_AddRoleAndPermissionFoundation", StringComparison.Ordinal));
            await dbContext.Database.MigrateAsync();
        }

        using var client = CreateSecureClient(application);
        var registration = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/patient-registrations",
            new RegisterPatientRequest
            {
                Email = PatientEmail,
                Password = PatientPassword,
            });
        Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);
        var originalRegistrationBody = await registration.Content.ReadAsStringAsync();
        var originalConfirmationCode = messages.Latest(
            IdentityMessageKind.PatientEmailConfirmation,
            PatientEmail).ActionCode;

        using var repeatedRegistration = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/patient-registrations",
            new RegisterPatientRequest
            {
                Email = PatientEmail,
                Password = PatientPassword,
            });
        Assert.Equal(HttpStatusCode.Accepted, repeatedRegistration.StatusCode);
        Assert.Equal(originalRegistrationBody, await repeatedRegistration.Content.ReadAsStringAsync());
        var confirmationCode = messages.Latest(
            IdentityMessageKind.PatientEmailConfirmation,
            PatientEmail).ActionCode;
        Assert.NotEqual(originalConfirmationCode, confirmationCode);

        using var revokedConfirmation = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/patient-email-confirmations",
            new ConfirmPatientEmailRequest
            {
                Email = PatientEmail,
                Code = originalConfirmationCode,
            });
        Assert.Equal(HttpStatusCode.BadRequest, revokedConfirmation.StatusCode);

        var prematureLogin = await LoginAsync(client, PatientEmail, PatientPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, prematureLogin.StatusCode);

        var confirmation = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/patient-email-confirmations",
            new ConfirmPatientEmailRequest
            {
                Email = PatientEmail,
                Code = confirmationCode,
            });
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);

        var login = await LoginAsync(client, PatientEmail, PatientPassword);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var setCookie = string.Join(
            ";",
            login.Headers.GetValues("Set-Cookie"));
        Assert.Contains("__Host-HospitalManagement.Auth=", setCookie, StringComparison.Ordinal);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(PatientPassword, setCookie, StringComparison.Ordinal);

        var account = await client.GetFromJsonAsync<CurrentAccountResponse>(
            "/api/v1/identity/session");
        Assert.NotNull(account);
        Assert.Equal(PatientEmail, account.Email);
        Assert.Equal(AccountKind.Patient.ToString(), account.AccountKind);

        var logout = await PostWithAntiforgeryAsync<object?>(
            client,
            "/api/v1/identity/sessions/logout",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/v1/identity/session")).StatusCode);

        await using var verificationScope = application.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<IdentityAccessDbContext>();
        var storedCodes = await verificationDb.ActionCodes.AsNoTracking().ToListAsync();
        Assert.Equal(2, storedCodes.Count);
        Assert.All(storedCodes, storedCode =>
        {
            Assert.Equal(64, storedCode.CodeHash.Length);
            Assert.DoesNotContain(confirmationCode, storedCode.CodeHash, StringComparison.Ordinal);
            Assert.DoesNotContain(originalConfirmationCode, storedCode.CodeHash, StringComparison.Ordinal);
        });
        Assert.Single(storedCodes, storedCode => storedCode.ConsumedAtUtc is not null);
        Assert.Single(storedCodes, storedCode => storedCode.RevokedAtUtc is not null);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F02-G02")]
    public async Task ResetResponsesDoNotEnumerateAccountsAndResetRevokesSessionsAndLockout()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();
        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);
        await MigrateIdentityAsync(application);
        using var client = CreateSecureClient(application);
        await CreateConfirmedPatientAsync(client, messages);

        var initialLogin = await LoginAsync(client, PatientEmail, PatientPassword);
        Assert.Equal(HttpStatusCode.OK, initialLogin.StatusCode);

        HttpResponseMessage? finalFailedLogin = null;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            finalFailedLogin?.Dispose();
            finalFailedLogin = await LoginAsync(client, PatientEmail, "DEMO-Incorrect-Password!9");
        }
        using (finalFailedLogin)
        {
            Assert.NotNull(finalFailedLogin);
            Assert.Equal(HttpStatusCode.Unauthorized, finalFailedLogin.StatusCode);
        }
        using var lockedLogin = await LoginAsync(client, PatientEmail, PatientPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, lockedLogin.StatusCode);

        using var knownResetRequest = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/password-reset-requests",
            new RequestPasswordResetRequest { Email = PatientEmail });
        using var unknownResetRequest = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/password-reset-requests",
            new RequestPasswordResetRequest { Email = "DEMO-unknown@hospital.invalid" });
        Assert.Equal(HttpStatusCode.Accepted, knownResetRequest.StatusCode);
        Assert.Equal(knownResetRequest.StatusCode, unknownResetRequest.StatusCode);
        Assert.Equal(
            await knownResetRequest.Content.ReadAsStringAsync(),
            await unknownResetRequest.Content.ReadAsStringAsync());

        var resetCode = messages.Latest(IdentityMessageKind.PasswordReset, PatientEmail).ActionCode;
        using var reset = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/password-resets",
            new ResetPasswordRequest
            {
                Email = PatientEmail,
                Code = resetCode,
                NewPassword = ReplacementPassword,
            });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/v1/identity/session")).StatusCode);

        using var reusedReset = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/password-resets",
            new ResetPasswordRequest
            {
                Email = PatientEmail,
                Code = resetCode,
                NewPassword = ReplacementPassword,
            });
        Assert.Equal(HttpStatusCode.BadRequest, reusedReset.StatusCode);

        using var oldPasswordLogin = await LoginAsync(client, PatientEmail, PatientPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);
        using var replacementLogin = await LoginAsync(client, PatientEmail, ReplacementPassword);
        Assert.Equal(HttpStatusCode.OK, replacementLogin.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F02-G02")]
    public async Task StaffAccountCanOnlyBeActivatedThroughSingleUseManagerInvitation()
    {
        const string StaffEmail = "DEMO-invited-staff@hospital.invalid";
        const string StaffPassword = "DEMO-Staff-Password!3";
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();
        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);
        await MigrateIdentityAsync(application);

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IIdentityLifecycleService>();
            var invitation = await service.InviteStaffAsync(new InviteStaffCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                StaffEmail));
            Assert.True(invitation.Succeeded);
        }

        using var client = CreateSecureClient(application);
        var invitationCode = messages.Latest(
            IdentityMessageKind.StaffInvitation,
            StaffEmail).ActionCode;
        using var acceptance = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/staff-invitation-acceptances",
            new AcceptStaffInvitationRequest
            {
                Email = StaffEmail,
                Code = invitationCode,
                Password = StaffPassword,
            });
        Assert.Equal(HttpStatusCode.OK, acceptance.StatusCode);

        using var replay = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/staff-invitation-acceptances",
            new AcceptStaffInvitationRequest
            {
                Email = StaffEmail,
                Code = invitationCode,
                Password = StaffPassword,
            });
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);

        using var staffLogin = await LoginAsync(client, StaffEmail, StaffPassword);
        Assert.Equal(HttpStatusCode.OK, staffLogin.StatusCode);
        var account = await client.GetFromJsonAsync<CurrentAccountResponse>(
            "/api/v1/identity/session");
        Assert.Equal(AccountKind.Staff.ToString(), account?.AccountKind);

        using var forbiddenSelfRegistration = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/staff-registrations",
            new
            {
                Email = "DEMO-self-staff@hospital.invalid",
                Password = StaffPassword
            });
        Assert.Equal(HttpStatusCode.MethodNotAllowed, forbiddenSelfRegistration.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F02-G02")]
    public async Task IdentityWritesWithoutAntiforgeryTokenAreRejectedBeforePersistence()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();
        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);
        await MigrateIdentityAsync(application);
        using var client = CreateSecureClient(application);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/identity/patient-registrations",
            new RegisterPatientRequest
            {
                Email = PatientEmail,
                Password = PatientPassword,
            });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(messages.Messages);

        await using var scope = application.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
        Assert.Empty(await dbContext.Users.AsNoTracking().ToListAsync());

        var cookie = application.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        Assert.Equal("__Host-HospitalManagement.Auth", cookie.Cookie.Name);
        Assert.True(cookie.Cookie.HttpOnly);
        Assert.Equal(Microsoft.AspNetCore.Http.CookieSecurePolicy.Always, cookie.Cookie.SecurePolicy);
        Assert.Equal(Microsoft.AspNetCore.Http.SameSiteMode.Strict, cookie.Cookie.SameSite);
        Assert.False(cookie.SlidingExpiration);
    }

    private static HttpClient CreateSecureClient(ApiWebApplicationFactory application) =>
        application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost", UriKind.Absolute),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    private static async Task MigrateIdentityAsync(ApiWebApplicationFactory application)
    {
        await using var scope = application.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
        await dbContext.Database.MigrateAsync();
    }

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
