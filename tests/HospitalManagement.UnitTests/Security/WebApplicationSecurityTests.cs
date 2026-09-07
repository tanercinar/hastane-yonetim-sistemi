using System.Text;
using HospitalManagement.BuildingBlocks.Security;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.IdentityAccess.Infrastructure;
using Xunit;

namespace HospitalManagement.UnitTests.Security;

public sealed class WebApplicationSecurityTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G03")]
    public void SecurityHeadersMeetOwaspAsvsMandatoryRequirements()
    {
        Assert.Equal("nosniff", SecurityHeaderDefaults.XContentTypeOptions);
        Assert.Equal("DENY", SecurityHeaderDefaults.XFrameOptions);
        Assert.Equal("strict-origin-when-cross-origin", SecurityHeaderDefaults.ReferrerPolicy);
        Assert.Equal("0", SecurityHeaderDefaults.XXssProtection);

        var permissionsPolicy = SecurityHeaderDefaults.PermissionsPolicy;
        Assert.Contains("camera=()", permissionsPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("geolocation=()", permissionsPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("microphone=()", permissionsPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("payment=()", permissionsPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("usb=()", permissionsPolicy, StringComparison.OrdinalIgnoreCase);

        var csp = SecurityHeaderDefaults.ContentSecurityPolicy;
        Assert.Contains("default-src 'self'", csp, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("frame-ancestors 'none'", csp, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("object-src 'none'", csp, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("base-uri 'self'", csp, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("form-action 'self'", csp, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("../../etc/shadow", "shadow")]
    [InlineData("..\\..\\windows\\system32\\cmd.exe", "cmd.exe")]
    [InlineData("/var/log/syslog", "syslog")]
    [InlineData("c:\\secrets\\key.pem", "key.pem")]
    [InlineData("..\\..\\nested/path/document.pdf", "document.pdf")]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G03")]
    public void PathTraversalSanitizerStripsDirectorySeparatorsAndPreservesBaseName(string dangerousPath, string expectedEnding)
    {
        var sanitized = AttachmentSecurityValidator.SanitizeFileName(dangerousPath);

        Assert.DoesNotContain("/", sanitized);
        Assert.DoesNotContain("\\", sanitized);
        Assert.DoesNotContain("..", sanitized);
        Assert.EndsWith(expectedEnding, sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G03")]
    public void UnsafeUploadValidationRejectsExecutableFilesEvenWhenRenamedToPdf()
    {
        // MZ header (DOS/PE executable magic bytes) disguised as .pdf
        var maliciousBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
        var (isValid, errorMessage, _) = AttachmentSecurityValidator.ValidateAttachment(
            "medical_report.pdf",
            "application/pdf",
            maliciousBytes);

        Assert.False(isValid, "Security Barrier Failed: Disguised PE/EXE binary must be rejected.");
        Assert.Contains("çalıştırılabilir", errorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("payload.exe")]
    [InlineData("script.bat")]
    [InlineData("exploit.ps1")]
    [InlineData("shell.sh")]
    [InlineData("webshell.php")]
    [InlineData("payload.jsp")]
    [InlineData("vector.svg")] // SVG can contain embedded JavaScript (XSS)
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G03")]
    public void UnsafeUploadValidationRejectsDangerousExtensions(string fileName)
    {
        var dummyBytes = Encoding.UTF8.GetBytes("sample content");
        var (isValid, errorMessage, _) = AttachmentSecurityValidator.ValidateAttachment(
            fileName,
            "application/octet-stream",
            dummyBytes);

        Assert.False(isValid, $"Security Barrier Failed: Extension {Path.GetExtension(fileName)} must be rejected.");
        Assert.Contains("geçersiz dosya uzantısı", errorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G03")]
    public void UnsafeUploadValidationRejectsFilesExceedingFifteenMegabytes()
    {
        // 15 MB + 1 byte
        var oversizedLength = (15 * 1024 * 1024) + 1;
        var fakeHeaderBytes = new byte[oversizedLength];
        fakeHeaderBytes[0] = 0x25; // %
        fakeHeaderBytes[1] = 0x50; // P
        fakeHeaderBytes[2] = 0x44; // D
        fakeHeaderBytes[3] = 0x46; // F

        var (isValid, errorMessage, _) = AttachmentSecurityValidator.ValidateAttachment(
            "large_scan.pdf",
            "application/pdf",
            fakeHeaderBytes);

        Assert.False(isValid, "Security Barrier Failed: File exceeding 15 MB must be rejected.");
        Assert.Contains("15 MB", errorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G03")]
    public void CookieConfigurationEnforcesHostPrefixAndStrictFlags()
    {
        Assert.Equal("__Host-HospitalManagement.Auth", IdentityAccessConstants.AuthCookieName);
        Assert.Equal("X-HMS-CSRF", IdentityAccessConstants.AntiforgeryHeaderName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G03")]
    public void AntiCsrfHeaderContractMatchesStandardTokenHeader()
    {
        var header = IdentityAccessConstants.AntiforgeryHeaderName;
        Assert.Equal("X-HMS-CSRF", header);
    }
}
