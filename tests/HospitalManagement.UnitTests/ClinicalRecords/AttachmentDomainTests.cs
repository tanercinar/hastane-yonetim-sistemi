using System.Text;
using HospitalManagement.BuildingBlocks.Storage;
using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.UnitTests.ClinicalRecords;

public sealed class AttachmentDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G07")]
    public void SanitizeFileNameStripsPathTraversalAndDangerousCharacters()
    {
        var raw1 = "../../../secret/medical_report.pdf";
        var clean1 = AttachmentSecurityValidator.SanitizeFileName(raw1);
        Assert.Equal("medical_report.pdf", clean1);

        var raw2 = "C:\\Windows\\System32\\test.dcm";
        var clean2 = AttachmentSecurityValidator.SanitizeFileName(raw2);
        Assert.Equal("test.dcm", clean2);

        var raw3 = "bad\0file:name*?.png";
        var clean3 = AttachmentSecurityValidator.SanitizeFileName(raw3);
        Assert.Equal("badfilename.png", clean3);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G07")]
    public void ValidateAttachmentAcceptsValidPdfAndComputesChecksum()
    {
        var validPdfBytes = "%PDF-1.5\n%Demo PDF Content\n%%EOF"u8.ToArray();
        var validation = AttachmentSecurityValidator.ValidateAttachment("test.pdf", "application/pdf", validPdfBytes);

        Assert.True(validation.IsValid);
        Assert.Null(validation.ErrorMessage);
        Assert.Equal("application/pdf", validation.ContentType);

        var sha256 = AttachmentSecurityValidator.ComputeSha256Checksum(validPdfBytes);
        Assert.Equal(64, sha256.Length);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G07")]
    public void ValidateAttachmentRejectsSpoofedPdfWithInvalidMagicBytes()
    {
        var spoofedBytes = "NOT A REAL PDF FILE HEADER"u8.ToArray();
        var validation = AttachmentSecurityValidator.ValidateAttachment("fake.pdf", "application/pdf", spoofedBytes);

        Assert.False(validation.IsValid);
        Assert.Contains("MIME / imza sahteciliği", validation.ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G07")]
    public void ValidateAttachmentRejectsExecutableFiles()
    {
        var exeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00 }; // MZ header
        var validation = AttachmentSecurityValidator.ValidateAttachment("malicious.pdf", "application/pdf", exeBytes);

        Assert.False(validation.IsValid);
        Assert.Contains("Çalıştırılabilir ikili (EXE/DLL)", validation.ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G07")]
    public void ValidateAttachmentRejectsDisallowedExtension()
    {
        var textBytes = "Plain text file content"u8.ToArray();
        var validation = AttachmentSecurityValidator.ValidateAttachment("script.sh", "text/plain", textBytes);

        Assert.False(validation.IsValid);
        Assert.Contains("Geçersiz dosya uzantısı", validation.ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G07")]
    public void ValidateAttachmentRejectsOverSizeLimit()
    {
        var largeBytes = new byte[16 * 1024 * 1024]; // 16 MB
        var validation = AttachmentSecurityValidator.ValidateAttachment("large.pdf", "application/pdf", largeBytes);

        Assert.False(validation.IsValid);
        Assert.Contains("15 MB sınırını aşıyor", validation.ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G07")]
    public void ValidateAttachmentRejectsDeclaredMimeMismatch()
    {
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        var validation = AttachmentSecurityValidator.ValidateAttachment(
            "image.png",
            "application/pdf",
            pngBytes);

        Assert.False(validation.IsValid);
        Assert.Contains("eşleşmiyor", validation.ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G07")]
    public void ValidateAttachmentRejectsDicomWithoutPart10Preamble()
    {
        var arbitraryBytes = new byte[256];

        var validation = AttachmentSecurityValidator.ValidateAttachment(
            "scan.dcm",
            "application/dicom",
            arbitraryBytes);

        Assert.False(validation.IsValid);
        Assert.Contains("DICOM Part 10", validation.ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G07")]
    public async Task MockMalwareScannerRejectsEicarMarker()
    {
        var scanner = new MockAttachmentMalwareScanner();
        var payload = Encoding.ASCII.GetBytes(
            "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE");

        var result = await scanner.ScanAsync(payload);

        Assert.False(result.IsClean);
        Assert.Equal("MOCK", result.ScannerMode);
        Assert.Equal("EICAR_TEST_MARKER", result.ThreatCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G07")]
    public void ClinicalAttachmentCreateAndMarkEnteredInError()
    {
        var id = Guid.NewGuid();
        var encId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var attachment = ClinicalAttachment.Create(
            id,
            encId,
            patientId,
            docId,
            ClinicalAttachmentType.RadiologyImage,
            "chest_xray.png",
            "patient/123/xray.png",
            "image/png",
            1024,
            "abc123sha",
            "Akciğer Grafisi",
            nowUtc);

        Assert.Equal(id, attachment.Id);
        Assert.Equal("chest_xray.png", attachment.FileName);
        Assert.False(attachment.IsEnteredInError);
        Assert.Equal(1, attachment.Version);

        attachment.MarkEnteredInError(docId, "Hatalı hasta grafisi yüklendi", nowUtc);
        Assert.True(attachment.IsEnteredInError);
        Assert.Equal("Hatalı hasta grafisi yüklendi", attachment.EnteredInErrorReason);
        Assert.Equal(2, attachment.Version);
    }
}
