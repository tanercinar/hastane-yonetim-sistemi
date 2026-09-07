using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.UnitTests.ClinicalRecords;

public sealed class ClinicalNoteDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G04")]
    public void CreateDraftInitializesDraftStatusAndProperties()
    {
        var id = Guid.NewGuid();
        var encounterId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var note = ClinicalNote.CreateDraft(
            id,
            encounterId,
            patientId,
            doctorId,
            ClinicalNoteType.GeneralSoap,
            "Poliklinik Muayene Notu",
            "Boğaz ağrısı ve ateş",
            "3 gündür devam eden şikayetler",
            "Orofarenks hiperemik, tonsiller hipertrofik",
            "Akut Tonsillit",
            "Semptomatik tedavi ve istirahat",
            "Hasta 3 gün sonra kontrole çağrıldı.",
            nowUtc);

        Assert.Equal(id, note.Id);
        Assert.Equal(encounterId, note.EncounterId);
        Assert.Equal(patientId, note.PatientId);
        Assert.Equal(doctorId, note.AuthorPractitionerId);
        Assert.Equal(ClinicalNoteType.GeneralSoap, note.NoteType);
        Assert.Equal(ClinicalNoteStatus.Draft, note.Status);
        Assert.Equal("Poliklinik Muayene Notu", note.Title);
        Assert.Equal("Boğaz ağrısı ve ateş", note.ChiefComplaint);
        Assert.Equal(1, note.Version);
        Assert.Null(note.SignedAtUtc);
        Assert.Null(note.SignedByPractitionerId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G04")]
    public void UpdateDraftModifiesFieldsAndIncrementsVersion()
    {
        var note = CreateSampleDraftNote();
        var nowUtc = DateTime.UtcNow;

        note.UpdateDraft(
            "Güncellenmiş Muayene Notu",
            "Boğaz ağrısı, hafif öksürük",
            "3 gündür süren şikayetler",
            "Fizik muayene bulguları güncellendi",
            "Akut Farenjit",
            "Oral hidrasyon ve pastil",
            "Ek açıklamalar girildi.",
            nowUtc);

        Assert.Equal("Güncellenmiş Muayene Notu", note.Title);
        Assert.Equal("Boğaz ağrısı, hafif öksürük", note.ChiefComplaint);
        Assert.Equal(2, note.Version);
        Assert.Equal(nowUtc, note.UpdatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G04")]
    public void SignTransitionsDraftToSigned()
    {
        var note = CreateSampleDraftNote();
        var doctorId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        note.Sign(doctorId, nowUtc);

        Assert.Equal(ClinicalNoteStatus.Signed, note.Status);
        Assert.Equal(doctorId, note.SignedByPractitionerId);
        Assert.Equal(nowUtc, note.SignedAtUtc);
        Assert.Equal(2, note.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G04")]
    public void UpdateDraftOnSignedNoteThrowsInvalidOperationException()
    {
        var note = CreateSampleDraftNote();
        note.Sign(Guid.NewGuid(), DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            note.UpdateDraft("Yasak Değişiklik", null, null, null, null, null, null, DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G04")]
    public void CreateAddendumOnSignedNoteSetsOriginalToAmendedAndProducesSignedAddendum()
    {
        var note = CreateSampleDraftNote();
        var doctorId = Guid.NewGuid();
        note.Sign(doctorId, DateTime.UtcNow);

        var addendumId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var addendum = note.CreateAddendum(
            addendumId,
            "Laboratuvar sonuçları görüldü, antibiyoterapi başlanmasına gerek duyulmadı.",
            "Tahlil sonucu değerlendirmesi",
            doctorId,
            nowUtc);

        Assert.Equal(ClinicalNoteStatus.Amended, note.Status);
        Assert.Equal(3, note.Version);

        Assert.Equal(addendumId, addendum.Id);
        Assert.Equal(note.Id, addendum.ParentNoteId);
        Assert.Equal(ClinicalNoteType.Addendum, addendum.NoteType);
        Assert.Equal(ClinicalNoteStatus.Signed, addendum.Status);
        Assert.Equal("Tahlil sonucu değerlendirmesi", addendum.CorrectionReason);
        Assert.Equal(doctorId, addendum.SignedByPractitionerId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G04")]
    public void CreateAddendumOnDraftNoteThrowsInvalidOperationException()
    {
        var note = CreateSampleDraftNote();

        Assert.Throws<InvalidOperationException>(() =>
            note.CreateAddendum(Guid.NewGuid(), "Ek içerik", "Gerekçe", Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G04")]
    public void MarkEnteredInErrorSetsStatusAndReason()
    {
        var note = CreateSampleDraftNote();
        var nowUtc = DateTime.UtcNow;

        note.MarkEnteredInError(Guid.NewGuid(), "Yanlış hasta dosyasına girilen not", nowUtc);

        Assert.Equal(ClinicalNoteStatus.EnteredInError, note.Status);
        Assert.Equal("Yanlış hasta dosyasına girilen not", note.EnteredInErrorReason);
        Assert.Equal(2, note.Version);
    }

    private static ClinicalNote CreateSampleDraftNote()
    {
        return ClinicalNote.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ClinicalNoteType.GeneralSoap,
            "İlk Muayene",
            "Halsizlik",
            "1 haftadır süren yorgunluk",
            "Normal",
            "Anemi şüphesi",
            "Hemogram istendi",
            "Detaylı notlar",
            DateTime.UtcNow);
    }
}
