using HospitalManagement.Modules.Patients.Domain;
using HospitalManagement.Modules.Reporting.Infrastructure;
using Xunit;

namespace HospitalManagement.UnitTests.Privacy;

public sealed class PrivacyReviewAndDataMinimizationTests
{
    private const string CanaryNationalId = "DEMO-CANARY-TR-12345678901";
    private const string CanaryFirstName = "CanaryFirst";
    private const string CanaryLastName = "CanaryLast";
    private const string CanaryPhone = "+905550001122";
    private const string CanaryEmail = "canary.patient@demo.invalid";
    private const string CanaryStreet = "Canary Mah. Gizli Sok. No:1";

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G10")]
    public void SyntheticCanaryMarkersAreIrreversiblyErasedUponPatientAnonymization()
    {
        var now = DateTime.UtcNow;
        var patientId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var address = new AddressValue(CanaryStreet, "Kadikoy", "Istanbul", "34710");

        var patient = Patient.Create(
            patientId,
            personId,
            "DEMO-MRN-2026-99999",
            CanaryFirstName,
            CanaryLastName,
            new DateOnly(1985, 5, 20),
            Gender.Female,
            CanaryNationalId,
            CanaryPhone,
            CanaryEmail,
            address,
            emergencyContact: null,
            communicationPreferences: null,
            nowUtc: now);

        // Pre-condition: Canary data exists
        Assert.Equal(CanaryFirstName, patient.FirstName);
        Assert.Equal(CanaryLastName, patient.LastName);
        Assert.Equal(CanaryNationalId, patient.NationalIdSynthetic);
        Assert.Equal(CanaryPhone, patient.PhoneNumber);
        Assert.Equal(CanaryEmail, patient.Email);
        Assert.NotNull(patient.Address);
        Assert.True(patient.IsActive);

        // Execute Right to Erasure / KVKK Anonymization
        patient.Anonymize(now.AddDays(30));

        // Invariant: Zero canary PII remains
        Assert.Equal("ANONİM", patient.FirstName);
        Assert.Equal("HASTA", patient.LastName);
        Assert.Null(patient.NationalIdSynthetic);
        Assert.Null(patient.PhoneNumber);
        Assert.Null(patient.Email);
        Assert.Null(patient.Address);
        Assert.Null(patient.EmergencyContact);
        Assert.False(patient.IsActive);
        Assert.False(patient.CommunicationPreferences.AllowEmail);
        Assert.False(patient.CommunicationPreferences.AllowSms);

        // Relational consistency: Identity keys are preserved for historical audit linking
        Assert.Equal(patientId, patient.Id);
        Assert.Equal("DEMO-MRN-2026-99999", patient.MedicalRecordNumber);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G10")]
    [InlineData("=cmd|'/C calc'!A0", "'=cmd|'/C calc'!A0")]
    [InlineData("@SUM(1+1)*cmd|' /C calc'!A0", "'@SUM(1+1)*cmd|' /C calc'!A0")]
    [InlineData("-2+3+cmd|' /C calc'!A0", "'-2+3+cmd|' /C calc'!A0")]
    [InlineData("+1+2+cmd|' /C calc'!A0", "'+1+2+cmd|' /C calc'!A0")]
    [InlineData("\t=2+5", "'\t=2+5")]
    [InlineData("Normal Metin", "Normal Metin")]
    [InlineData("Metin, virgüle sahip", "\"Metin, virgüle sahip\"")]
    public void SecureCsvExportSanitizesFormulaInjectionAndEscapesSpecialCharacters(
        string input,
        string expected)
    {
        var sanitized = SecureExportService.SanitizeCsvCell(input);
        Assert.Equal(expected, sanitized);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G10")]
    public void SecureCsvExportEnforcesStrictRowCapToPreventMemoryExhaustion()
    {
        Assert.Equal(5000, SecureExportService.MaxExportRows);
    }
}
