using HospitalManagement.Modules.Patients.Application;
using HospitalManagement.Modules.Patients.Domain;

namespace HospitalManagement.UnitTests;

public sealed class PatientDomainUnitTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G01")]
    public void PatientCreationInitializesPropertiesCorrectly()
    {
        var now = DateTime.UtcNow;
        var id = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var patient = Patient.Create(
            id,
            personId,
            "mrn-2026-000042 ",
            " Fatma ",
            " Kaya ",
            new DateOnly(1990, 5, 20),
            Gender.Female,
            " 99900000002 ",
            " +90 555 123 4567 ",
            " DEMO-patient@hospital.invalid ",
            new AddressValue("Ankara", "Çankaya", "Tunalı No: 5"),
            new EmergencyContactValue("Ali Kaya", "Babası", "+90 555 999 8877"),
            new CommunicationPreferencesValue(AllowSms: true, AllowEmail: false),
            now);

        Assert.Equal(id, patient.Id);
        Assert.Equal(personId, patient.PersonId);
        Assert.Equal("MRN-2026-000042", patient.MedicalRecordNumber);
        Assert.Equal("Fatma", patient.FirstName);
        Assert.Equal("Kaya", patient.LastName);
        Assert.Equal(new DateOnly(1990, 5, 20), patient.DateOfBirth);
        Assert.Equal(Gender.Female, patient.Gender);
        Assert.Equal("99900000002", patient.NationalIdSynthetic);
        Assert.Equal("+90 555 123 4567", patient.PhoneNumber);
        Assert.Equal("demo-patient@hospital.invalid", patient.Email);
        Assert.NotNull(patient.Address);
        Assert.Equal("Ankara", patient.Address.City);
        Assert.NotNull(patient.EmergencyContact);
        Assert.True(patient.CommunicationPreferences.AllowSms);
        Assert.False(patient.CommunicationPreferences.AllowEmail);
        Assert.True(patient.IsActive);
        Assert.Equal(now, patient.CreatedAtUtc);
        Assert.Equal(1, patient.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G01")]
    public void UpdatingDemographicsUpdatesPropertiesAndTimestamp()
    {
        var createTime = DateTime.UtcNow.AddHours(-1);
        var updateTime = DateTime.UtcNow;

        var patient = Patient.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "MRN-2026-000001",
            "Ahmet",
            "Demir",
            new DateOnly(1980, 1, 1),
            Gender.Male,
            null,
            null,
            null,
            null,
            null,
            null,
            createTime);

        patient.UpdateDemographics(
            "Ahmet Can",
            "Demir",
            new DateOnly(1980, 1, 1),
            Gender.Male,
            "99900000003",
            "+90 555 444 3322",
            "DEMO-ahmet@hospital.invalid",
            new AddressValue("İzmir", "Konak", "Kordon No: 10"),
            null,
            null,
            updateTime);

        Assert.Equal("Ahmet Can", patient.FirstName);
        Assert.Equal("99900000003", patient.NationalIdSynthetic);
        Assert.Equal("+90 555 444 3322", patient.PhoneNumber);
        Assert.Equal("demo-ahmet@hospital.invalid", patient.Email);
        Assert.Equal(updateTime, patient.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("99900000001", "99*******01")]
    [InlineData("1234", "****")]
    [InlineData("12", "****")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G01")]
    public void MaskNationalIdMasksExpectedDigits(string? input, string? expected)
    {
        var result = PatientMaskingHelper.MaskNationalId(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("+905551234567", "+905*******67")]
    [InlineData("05551234567", "0555*****67")]
    [InlineData("12345", "******")]
    [InlineData(null, null)]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G01")]
    public void MaskPhoneNumberMasksExpectedDigits(string? input, string? expected)
    {
        var result = PatientMaskingHelper.MaskPhoneNumber(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G01")]
    public void MedicalRecordNumberGeneratorFormatsCorrectly()
    {
        var mrn = MedicalRecordNumberGenerator.Generate(2026, 7);
        Assert.Equal("MRN-2026-000007", mrn);
    }
}
