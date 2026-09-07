using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.UnitTests.ClinicalRecords;

public sealed class AllergyDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G02")]
    public void CreateAllergyInitializesActiveStatusAndProperties()
    {
        var id = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var allergy = AllergyIntolerance.Create(
            id,
            patientId,
            null,
            "Penisilin",
            AllergyCategory.Medication,
            AllergyCriticality.High,
            "Ürtiker ve anafilaksi",
            nowUtc.AddYears(-2),
            "Çocukluktan beri mevcut",
            doctorId,
            nowUtc);

        Assert.Equal(id, allergy.Id);
        Assert.Equal(patientId, allergy.PatientId);
        Assert.Equal("Penisilin", allergy.Substance);
        Assert.Equal(AllergyCategory.Medication, allergy.Category);
        Assert.Equal(AllergyCriticality.High, allergy.Criticality);
        Assert.Equal(AllergyClinicalStatus.Active, allergy.ClinicalStatus);
        Assert.Equal(AllergyVerificationStatus.Confirmed, allergy.VerificationStatus);
        Assert.Equal("Ürtiker ve anafilaksi", allergy.Manifestation);
        Assert.Equal(1, allergy.Version);
        Assert.Null(allergy.EnteredInErrorReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G02")]
    public void UpdateAllergyStatusChangesStatusAndIncrementsVersion()
    {
        var allergy = CreateSampleAllergy();
        var nowUtc = DateTime.UtcNow;

        allergy.UpdateStatus(AllergyClinicalStatus.Resolved, "Desensitizasyon tamamlandı", Guid.NewGuid(), nowUtc);

        Assert.Equal(AllergyClinicalStatus.Resolved, allergy.ClinicalStatus);
        Assert.Equal("Desensitizasyon tamamlandı", allergy.Notes);
        Assert.Equal(2, allergy.Version);
        Assert.Equal(nowUtc, allergy.UpdatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G02")]
    public void MarkEnteredInErrorSetsVerificationStatusAndReason()
    {
        var allergy = CreateSampleAllergy();
        var nowUtc = DateTime.UtcNow;

        allergy.MarkEnteredInError(Guid.NewGuid(), "Yanlış hasta dosyasına girildi", nowUtc);

        Assert.Equal(AllergyVerificationStatus.EnteredInError, allergy.VerificationStatus);
        Assert.Equal("Yanlış hasta dosyasına girildi", allergy.EnteredInErrorReason);
        Assert.Equal(2, allergy.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G02")]
    public void UpdateStatusOnEnteredInErrorThrowsInvalidOperationException()
    {
        var allergy = CreateSampleAllergy();
        allergy.MarkEnteredInError(Guid.NewGuid(), "Hata", DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            allergy.UpdateStatus(AllergyClinicalStatus.Resolved, null, Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G02")]
    public void EmptySubstanceOrPatientThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            AllergyIntolerance.Create(
                Guid.NewGuid(),
                Guid.Empty,
                null,
                "Aspirin",
                AllergyCategory.Medication,
                AllergyCriticality.Low,
                null,
                null,
                null,
                Guid.NewGuid(),
                DateTime.UtcNow));

        Assert.Throws<ArgumentException>(() =>
            AllergyIntolerance.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                "  ",
                AllergyCategory.Medication,
                AllergyCriticality.Low,
                null,
                null,
                null,
                Guid.NewGuid(),
                DateTime.UtcNow));
    }

    private static AllergyIntolerance CreateSampleAllergy()
    {
        return AllergyIntolerance.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Lateks",
            AllergyCategory.Environment,
            AllergyCriticality.Low,
            "Temas dermatiti",
            DateTime.UtcNow,
            null,
            Guid.NewGuid(),
            DateTime.UtcNow);
    }
}
