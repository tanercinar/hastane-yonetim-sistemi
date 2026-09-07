using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.UnitTests.ClinicalRecords;

public sealed class DiagnosisDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G05")]
    public void CreateCodedDiagnosisInitializesPropertiesAndSetsIsCodedTrue()
    {
        var id = Guid.NewGuid();
        var encounterId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var diagnosis = EncounterDiagnosis.CreateCoded(
            id,
            encounterId,
            patientId,
            doctorId,
            DiagnosisType.Final,
            "J06.9",
            "Akut üst solunum yolu enfeksiyonu, tanımlanmamış",
            "ICD-10-TR-2026.1",
            "Semptomatik tedavi başlandı",
            nowUtc);

        Assert.Equal(id, diagnosis.Id);
        Assert.Equal(encounterId, diagnosis.EncounterId);
        Assert.Equal(patientId, diagnosis.PatientId);
        Assert.Equal(doctorId, diagnosis.DiagnosedByPractitionerId);
        Assert.Equal(DiagnosisType.Final, diagnosis.DiagnosisType);
        Assert.True(diagnosis.IsCoded);
        Assert.Equal("J06.9", diagnosis.Icd10Code);
        Assert.Equal("Akut üst solunum yolu enfeksiyonu, tanımlanmamış", diagnosis.DiagnosisTitle);
        Assert.Equal("ICD-10-TR-2026.1", diagnosis.CatalogVersion);
        Assert.Equal("Semptomatik tedavi başlandı", diagnosis.Notes);
        Assert.False(diagnosis.IsEnteredInError);
        Assert.Equal(1, diagnosis.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G05")]
    public void CreateFreeTextDiagnosisInitializesPropertiesAndSetsIsCodedFalse()
    {
        var id = Guid.NewGuid();
        var encounterId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var diagnosis = EncounterDiagnosis.CreateFreeText(
            id,
            encounterId,
            patientId,
            doctorId,
            DiagnosisType.Differential,
            "Açıklanamayan göğüs ağrısı",
            "Kardiyoloji konsültasyonu gerekebilir",
            nowUtc);

        Assert.Equal(id, diagnosis.Id);
        Assert.Equal(DiagnosisType.Differential, diagnosis.DiagnosisType);
        Assert.False(diagnosis.IsCoded);
        Assert.Null(diagnosis.Icd10Code);
        Assert.Null(diagnosis.CatalogVersion);
        Assert.Equal("Açıklanamayan göğüs ağrısı", diagnosis.DiagnosisTitle);
        Assert.Equal(1, diagnosis.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G05")]
    public void CreateCodedDiagnosisWithoutCodeThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            EncounterDiagnosis.CreateCoded(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DiagnosisType.Preliminary,
                "",
                "Tanı Başlığı",
                "ICD-10-TR-2026.1",
                null,
                DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G05")]
    public void UpdateDiagnosisModifiesPropertiesAndIncrementsVersion()
    {
        var diagnosis = EncounterDiagnosis.CreateFreeText(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DiagnosisType.Preliminary,
            "Ön Tanı",
            null,
            DateTime.UtcNow);

        var nowUtc = DateTime.UtcNow;
        diagnosis.Update(
            DiagnosisType.Final,
            true,
            "I10",
            "Esansiyel hipertansiyon",
            "ICD-10-TR-2026.1",
            "Kesinleşen tanı",
            nowUtc);

        Assert.Equal(DiagnosisType.Final, diagnosis.DiagnosisType);
        Assert.True(diagnosis.IsCoded);
        Assert.Equal("I10", diagnosis.Icd10Code);
        Assert.Equal("Esansiyel hipertansiyon", diagnosis.DiagnosisTitle);
        Assert.Equal(2, diagnosis.Version);
        Assert.Equal(nowUtc, diagnosis.UpdatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G05")]
    public void MarkEnteredInErrorSetsFlagAndReasonAndLocksUpdate()
    {
        var diagnosis = EncounterDiagnosis.CreateFreeText(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DiagnosisType.Preliminary,
            "Ön Tanı",
            null,
            DateTime.UtcNow);

        var nowUtc = DateTime.UtcNow;
        diagnosis.MarkEnteredInError(Guid.NewGuid(), "Yanlış tanı kaydı", nowUtc);

        Assert.True(diagnosis.IsEnteredInError);
        Assert.Equal("Yanlış tanı kaydı", diagnosis.EnteredInErrorReason);
        Assert.Equal(2, diagnosis.Version);

        Assert.Throws<InvalidOperationException>(() =>
            diagnosis.Update(DiagnosisType.Final, false, null, "Yeni Başlık", null, null, DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G05")]
    public void FinalDiagnosisCannotBeSilentlyUpdated()
    {
        var diagnosis = EncounterDiagnosis.CreateCoded(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DiagnosisType.Final,
            "I10",
            "Esansiyel hipertansiyon",
            "ICD-10-TR-2026.1",
            null,
            DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => diagnosis.Update(
            DiagnosisType.Final,
            true,
            "I11",
            "Hipertansif kalp hastalığı",
            "ICD-10-TR-2026.1",
            null,
            DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G05")]
    public void DiagnosisCatalogItemCreateInitializesProperties()
    {
        var id = Guid.NewGuid();
        var item = DiagnosisCatalogItem.Create(
            id,
            "j03.9",
            "Akut tonsillit, tanımlanmamış",
            "Acute tonsillitis, unspecified",
            "X - Solunum Sistemi Hastalıkları",
            "J00-J06 Akut üst solunum yolu enfeksiyonları",
            "ICD-10-TR-2026.1",
            true);

        Assert.Equal(id, item.Id);
        Assert.Equal("J03.9", item.Code);
        Assert.Equal("Akut tonsillit, tanımlanmamış", item.NameTurkish);
        Assert.Equal("Acute tonsillitis, unspecified", item.NameEnglish);
        Assert.Equal("ICD-10-TR-2026.1", item.CatalogVersion);
        Assert.True(item.IsActive);
    }
}
