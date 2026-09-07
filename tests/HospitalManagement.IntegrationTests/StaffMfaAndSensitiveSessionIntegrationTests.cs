using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;

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

public sealed class StaffMfaAndSensitiveSessionIntegrationTests
{
    private const string StaffEmail = "DEMO-staff-mfa@hospital.invalid";
    private const string StaffPassword = "DEMO-Staff-Pass!1";

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F02-G05")]
    public async Task StaffCanEnableMfaLoginWithTotpAndRecoveryCodesAndEnforceSensitiveSession()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        // 1. Personel kullanıcısı oluştur
        var staffPersonId = Guid.NewGuid();
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var staffUser = ApplicationUser.CreateInvitedStaff(
                Guid.NewGuid(),
                staffPersonId,
                StaffEmail,
                DateTime.UtcNow);
            staffUser.AcceptStaffInvitation();

            var createResult = await userManager.CreateAsync(staffUser, StaffPassword);
            Assert.True(createResult.Succeeded);

            var addRoleResult = await userManager.AddToRoleAsync(staffUser, HospitalRoles.Doctor);
            Assert.True(addRoleResult.Succeeded);
        }

        using var client = CreateSecureClient(application);

        // 2. İlk giriş (MFA henüz etkin değil)
        var firstLogin = await LoginAsync(client, StaffEmail, StaffPassword);
        Assert.Equal(HttpStatusCode.OK, firstLogin.StatusCode);
        var loginBody = await firstLogin.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginBody);
        Assert.False(loginBody.RequiresTwoFactor);

        // 3. MFA Kurulum Bilgilerini Al
        var mfaSetupResponse = await client.GetAsync("/api/v1/identity/mfa/setup");
        Assert.Equal(HttpStatusCode.OK, mfaSetupResponse.StatusCode);
        var mfaSetup = await mfaSetupResponse.Content.ReadFromJsonAsync<MfaSetupResponse>();
        Assert.NotNull(mfaSetup);
        Assert.NotEmpty(mfaSetup.SharedKey);
        Assert.Contains(mfaSetup.SharedKey, mfaSetup.AuthenticatorUri);

        // 4. MFA'yı Etkinleştir (Geçersiz kod ile ret, geçerli TOTP ile onay)
        var invalidEnableResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/mfa/enable",
            new EnableMfaRequest { VerificationCode = "000000" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidEnableResponse.StatusCode);

        var validCode = GenerateTotpCode(mfaSetup.SharedKey);
        var enableResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/mfa/enable",
            new EnableMfaRequest { VerificationCode = validCode });
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
        var recoveryCodesResponse = await enableResponse.Content.ReadFromJsonAsync<MfaRecoveryCodesResponse>();
        Assert.NotNull(recoveryCodesResponse);
        Assert.Equal(8, recoveryCodesResponse.RecoveryCodes.Count);
        var recoveryCodes = recoveryCodesResponse.RecoveryCodes;

        // Oturum durumunu kontrol et (TwoFactorEnabled == true)
        var sessionResponse = await client.GetAsync("/api/v1/identity/session");
        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);
        var session = await sessionResponse.Content.ReadFromJsonAsync<CurrentAccountResponse>();
        Assert.NotNull(session);
        Assert.True(session.TwoFactorEnabled);

        // 5. Çıkış Yap ve İki Faktörlü Giriş Akışını Test Et
        await PostWithAntiforgeryAsync<object?>(client, "/api/v1/identity/sessions/logout", null);

        // Parola ile ilk adım: 200 OK + RequiresTwoFactor = true
        var mfaLoginStep1 = await LoginAsync(client, StaffEmail, StaffPassword);
        Assert.Equal(HttpStatusCode.OK, mfaLoginStep1.StatusCode);
        var step1Body = await mfaLoginStep1.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(step1Body);
        Assert.True(step1Body.RequiresTwoFactor);

        // Henüz tam oturum açılmadı
        var unverifiedSession = await client.GetAsync("/api/v1/identity/session");
        Assert.Equal(HttpStatusCode.Unauthorized, unverifiedSession.StatusCode);

        // Yanlış 2FA kodu: 401 Unauthorized
        var wrong2faResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/two-factor-sessions",
            new TwoFactorLoginRequest { Code = "123456", IsRecoveryCode = false });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong2faResponse.StatusCode);

        // Doğru 2FA kodu: 200 OK
        var currentTotp = GenerateTotpCode(mfaSetup.SharedKey);
        var correct2faResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/two-factor-sessions",
            new TwoFactorLoginRequest { Code = currentTotp, IsRecoveryCode = false });
        Assert.Equal(HttpStatusCode.OK, correct2faResponse.StatusCode);

        // Artık oturum doğrulanmış ve aktif
        var verifiedSession = await client.GetAsync("/api/v1/identity/session");
        Assert.Equal(HttpStatusCode.OK, verifiedSession.StatusCode);

        // 6. Kurtarma Kodu ile Giriş ve Tek Kullanımlık İlkesinin Denetlenmesi
        await PostWithAntiforgeryAsync<object?>(client, "/api/v1/identity/sessions/logout", null);

        var recoveryLoginStep1 = await LoginAsync(client, StaffEmail, StaffPassword);
        Assert.Equal(HttpStatusCode.OK, recoveryLoginStep1.StatusCode);

        // 1. Kurtarma kodu ile giriş yap (Başarılı)
        var usedRecoveryCode = recoveryCodes[0];
        var recoveryLoginResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/two-factor-sessions",
            new TwoFactorLoginRequest { Code = usedRecoveryCode, IsRecoveryCode = true });
        Assert.Equal(HttpStatusCode.OK, recoveryLoginResponse.StatusCode);

        // Çıkış yap ve aynı kurtarma kodunu tekrar kullanmayı dene
        await PostWithAntiforgeryAsync<object?>(client, "/api/v1/identity/sessions/logout", null);
        await LoginAsync(client, StaffEmail, StaffPassword);

        var reusedRecoveryResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/two-factor-sessions",
            new TwoFactorLoginRequest { Code = usedRecoveryCode, IsRecoveryCode = true });
        // Aynı kod tekrar kullanılamaz (401 Unauthorized - Tek kullanımlık ilke!)
        Assert.Equal(HttpStatusCode.Unauthorized, reusedRecoveryResponse.StatusCode);

        // 2. Kurtarma kodu ile giriş yap (Başarılı)
        var secondRecoveryResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/two-factor-sessions",
            new TwoFactorLoginRequest { Code = recoveryCodes[1], IsRecoveryCode = true });
        Assert.Equal(HttpStatusCode.OK, secondRecoveryResponse.StatusCode);

        // 7. Hassas İşlem / Yakın Zamanda Doğrulanmış Oturum Denetimi
        // Yeni giriş yapılmış oturum ile hassas işlem başarılı (200 OK)
        var sensitiveResponse = await client.PostAsync(
            "/api/v1/platform/sensitive-probes/sensitive-operation",
            null);
        Assert.Equal(HttpStatusCode.OK, sensitiveResponse.StatusCode);

        // 8. Kurtarma Kodlarını Yeniden Üret
        var regenResponse = await PostWithAntiforgeryAsync<object?>(
            client,
            "/api/v1/identity/mfa/recovery-codes",
            null);
        Assert.Equal(HttpStatusCode.OK, regenResponse.StatusCode);
        var regenerated = await regenResponse.Content.ReadFromJsonAsync<MfaRecoveryCodesResponse>();
        Assert.NotNull(regenerated);
        Assert.Equal(8, regenerated.RecoveryCodes.Count);

        // 9. Yanlış Parola ile MFA Kapatma (401 Unauthorized)
        var wrongDisable = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/mfa/disable",
            new DisableMfaRequest { Password = "Wrong-Password-1!" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongDisable.StatusCode);

        // 10. Doğru Parola ile MFA Kapatma (200 OK)
        var correctDisable = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/mfa/disable",
            new DisableMfaRequest { Password = StaffPassword });
        Assert.Equal(HttpStatusCode.OK, correctDisable.StatusCode);

        // 11. Oturum durumunu doğrula (TwoFactorEnabled == false)
        var disabledSessionResponse = await client.GetAsync("/api/v1/identity/session");
        Assert.Equal(HttpStatusCode.OK, disabledSessionResponse.StatusCode);
        var disabledSession = await disabledSessionResponse.Content.ReadFromJsonAsync<CurrentAccountResponse>();
        Assert.NotNull(disabledSession);
        Assert.False(disabledSession.TwoFactorEnabled);

        // 12. Çıkış yap ve doğrudan parola ile giriş yapabilmeyi doğrula (2FA istenmez)
        await PostWithAntiforgeryAsync<object?>(client, "/api/v1/identity/sessions/logout", null);
        var directLogin = await LoginAsync(client, StaffEmail, StaffPassword);
        Assert.Equal(HttpStatusCode.OK, directLogin.StatusCode);
        var directBody = await directLogin.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(directBody);
        Assert.False(directBody.RequiresTwoFactor);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "RFC 6238 TOTP requires HMACSHA1.")]
    private static string GenerateTotpCode(string base32Key, DateTimeOffset? time = null)
    {
        var keyBytes = Base32Decode(base32Key);
        var timestamp = (time ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / 30;
        var timestampBytes = BitConverter.GetBytes(timestamp);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timestampBytes);
        }

        using var hmac = new HMACSHA1(keyBytes);
        var hash = hmac.ComputeHash(timestampBytes);
        var offset = hash[^1] & 0x0F;
        var binaryCode = ((hash[offset] & 0x7F) << 24)
                       | ((hash[offset + 1] & 0xFF) << 16)
                       | ((hash[offset + 2] & 0xFF) << 8)
                       | (hash[offset + 3] & 0xFF);
        var otp = binaryCode % 1000000;
        return otp.ToString("D6", CultureInfo.InvariantCulture);
    }

    private static byte[] Base32Decode(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var clean = base32.TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>();
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var c in clean)
        {
            var val = alphabet.IndexOf(c, StringComparison.Ordinal);
            if (val < 0)
            {
                continue;
            }
            buffer = (buffer << 5) | val;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                output.Add((byte)(buffer >> (bitsLeft - 8)));
                bitsLeft -= 8;
            }
        }
        return output.ToArray();
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

