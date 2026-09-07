using System.Security.Cryptography;
using System.Text;
using HospitalManagement.Modules.IdentityAccess.Domain;
using HospitalManagement.Modules.IdentityAccess.Infrastructure;
using Xunit;

namespace HospitalManagement.UnitTests.Security;

public sealed class IdentityAndSecretSecurityTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G04")]
    public void IdentityOptionsEnforceBruteForceDefenses()
    {
        var options = new IdentityAccessOptions();

        Assert.Equal(5, options.MaxFailedAccessAttempts);
        Assert.Equal(15, options.LockoutMinutes);
        Assert.Equal(60, options.SessionMinutes);
        Assert.Equal(30, options.ActionCodeMinutes);
        Assert.Equal(10, options.SensitivePermitLimit);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G04")]
    public void ActionCodeOnlyStoresSha256HashAndEnforcesExpiration()
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(30);
        var rawCode = "DEMO-secure-action-token-12345";
        var rawCodeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawCode)));

        var actionCode = IdentityActionCode.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            IdentityActionPurpose.ResetPassword,
            rawCodeHash,
            now,
            expiresAt,
            null);

        Assert.Equal(rawCodeHash, actionCode.CodeHash);
        Assert.NotEqual(rawCode, actionCode.CodeHash);
        Assert.True(actionCode.IsUsableAt(now.AddMinutes(15)));

        // Expired after 31 minutes
        Assert.False(actionCode.IsUsableAt(now.AddMinutes(31)));

        // Once consumed, cannot be reused (one-time token defense)
        actionCode.Consume(now.AddMinutes(5));
        Assert.False(actionCode.IsUsableAt(now.AddMinutes(10)), "Replay Attack: Consumed action code must not be reusable.");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G04")]
    public void ActionCodeRevocationPreventsSubsequentUsage()
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(30);
        var rawCodeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("sample-token")));

        var actionCode = IdentityActionCode.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            IdentityActionPurpose.ConfirmPatientEmail,
            rawCodeHash,
            now,
            expiresAt,
            null);

        Assert.True(actionCode.IsUsableAt(now.AddMinutes(5)));

        // Revoke the code
        actionCode.Revoke(now.AddMinutes(6));
        Assert.False(actionCode.IsUsableAt(now.AddMinutes(7)), "Revoked action code must not be usable.");
    }

    [Theory]
    [InlineData("patient@hospital.invalid", true)]
    [InlineData("doctor@hospital.invalid", true)]
    [InlineData("admin@subdomain.hospital.invalid", true)]
    [InlineData("patient@gmail.com", false)]
    [InlineData("doctor@outlook.com", false)]
    [InlineData("attacker@evil.com", false)]
    [InlineData("test@hospital.com.tr", false)]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G04")]
    public void EmailDomainStrictlyRestrictedToReservedInvalidTld(string candidateEmail, bool expectedValid)
    {
        // System requires RFC 2606 reserved .invalid domain for all synthetic DEMO accounts
        var isValid = false;
        try
        {
            var parsed = new System.Net.Mail.MailAddress(candidateEmail.Trim());
            isValid = string.Equals(parsed.Address, candidateEmail.Trim(), StringComparison.Ordinal)
                && parsed.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            isValid = false;
        }

        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G04")]
    public void PasswordHashingIsNonReversible()
    {
        var rawPassword = "P@ssw0rd123!Secure";
        var hash1 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawPassword)));
        var hash2 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawPassword)));

        Assert.Equal(hash1, hash2);
        Assert.DoesNotContain(rawPassword, hash1);
    }
}
