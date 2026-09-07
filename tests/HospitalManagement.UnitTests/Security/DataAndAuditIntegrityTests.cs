using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.AuditPrivacy.Domain;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.Patients.Domain;
using Xunit;

namespace HospitalManagement.UnitTests.Security;

public sealed class DataAndAuditIntegrityTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G05")]
    public void ClinicalSignedNoteCannotBeMutatedDirectlyAndRequiresAddendum()
    {
        var now = DateTime.UtcNow;
        var note = ClinicalNote.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ClinicalNoteType.ProgressNote,
            "Initial Clinical Assessment",
            "Headache",
            "Onset 2 days ago",
            "Vitals normal",
            "Tension headache",
            "Rest and hydration",
            "Full clinical narrative",
            now);

        // Sign the note
        note.Sign(Guid.NewGuid(), now.AddMinutes(10));
        Assert.Equal(ClinicalNoteStatus.Signed, note.Status);

        // Verify direct draft updates are strictly prohibited
        Assert.Throws<InvalidOperationException>(() =>
            note.UpdateDraft(
                "Tampered Title",
                "Tampered Complaint",
                "Tampered History",
                "Tampered Exam",
                "Tampered Assessment",
                "Tampered Plan",
                "Tampered Content",
                now.AddMinutes(20)));

        // Verify that amendments require explicit addendum creation
        var addendum = note.CreateAddendum(
            Guid.NewGuid(),
            "Patient reports headache resolved after rest.",
            "Follow-up clarification",
            Guid.NewGuid(),
            now.AddMinutes(30));

        Assert.Equal(ClinicalNoteStatus.Amended, note.Status);
        Assert.Equal(note.Id, addendum.ParentNoteId);
        Assert.Equal(ClinicalNoteStatus.Signed, addendum.Status);
        Assert.Equal("Follow-up clarification", addendum.CorrectionReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G05")]
    public void ClinicalNoteEnteredInErrorRequiresExplicitReasonAndPreservesRecord()
    {
        var now = DateTime.UtcNow;
        var practitionerId = Guid.NewGuid();
        var note = ClinicalNote.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            practitionerId,
            ClinicalNoteType.Consultation,
            "Cardiology Consult",
            "Chest pain",
            "History of present illness",
            "Physical exam details",
            "Assessment details",
            "Plan details",
            "Note content",
            now);

        note.Sign(practitionerId, now.AddMinutes(5));

        // Mark entered in error with reason
        note.MarkEnteredInError(practitionerId, "Entered on incorrect patient file by clerical error", now.AddMinutes(15));

        Assert.Equal(ClinicalNoteStatus.EnteredInError, note.Status);
        Assert.Equal("Entered on incorrect patient file by clerical error", note.EnteredInErrorReason);
        // Original clinical narrative is retained for legal auditing, not wiped or deleted
        Assert.Equal("Note content", note.Content);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G05")]
    public void AuditLogChainedHashIntegrityDetectsTampering()
    {
        var now = DateTime.UtcNow;
        var entryId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var actorPersonId = Guid.NewGuid();

        var entry = AuditLogEntry.Create(
            entryId,
            now,
            actorUserId,
            actorPersonId,
            "Doctor",
            "127.0.0.1",
            "AgentRunner/1.0",
            "ClinicalRecords.ClinicalNoteSign",
            "ClinicalNote",
            Guid.NewGuid().ToString(),
            AuditOutcome.Success,
            "Doctor signed clinical note",
            Guid.NewGuid().ToString("N"),
            null,
            previousRecordHash: "0000000000000000000000000000000000000000000000000000000000000000");

        // Legitimate entry validates hash integrity
        Assert.True(entry.VerifyHashIntegrity());

        // Tamper with target resource type using reflection
        var targetTypeProperty = typeof(AuditLogEntry).GetProperty("TargetResourceType");
        Assert.NotNull(targetTypeProperty);
        targetTypeProperty.SetValue(entry, "TamperedResource");

        // Verify that tampering is immediately detected by hash mismatch
        Assert.False(entry.VerifyHashIntegrity(), "Tamper Detection Failed: Altered audit record must fail integrity check.");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G05")]
    public void AuditLogPreviousRecordHashChainsSequentialEntries()
    {
        var now = DateTime.UtcNow;
        var entry1 = AuditLogEntry.Create(
            Guid.NewGuid(),
            now,
            Guid.NewGuid(),
            null,
            "System",
            "127.0.0.1",
            "TestRunner",
            "Identity.UserLogin",
            "User",
            Guid.NewGuid().ToString(),
            AuditOutcome.Success,
            null,
            Guid.NewGuid().ToString("N"));

        var entry2 = AuditLogEntry.Create(
            Guid.NewGuid(),
            now.AddSeconds(1),
            Guid.NewGuid(),
            null,
            "Doctor",
            "127.0.0.1",
            "TestRunner",
            "ClinicalRecords.ClinicalNoteSign",
            "ClinicalNote",
            Guid.NewGuid().ToString(),
            AuditOutcome.Success,
            null,
            Guid.NewGuid().ToString("N"),
            previousRecordHash: entry1.RecordHash);

        Assert.True(entry1.VerifyHashIntegrity());
        Assert.True(entry2.VerifyHashIntegrity());
        Assert.Equal(entry1.RecordHash, entry2.PreviousRecordHash);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G05")]
    public void PatientDataRetentionAnonymizationWipesPiiWhilePreservingRelationalIntegrity()
    {
        var now = DateTime.UtcNow;
        var patientId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var patient = Patient.Create(
            patientId,
            personId,
            "MRN-2026-9999",
            "Can",
            "Yılmaz",
            new DateOnly(1985, 5, 20),
            Gender.Male,
            "DEMO-12345678901",
            "05551234567",
            "can.yilmaz@hospital.invalid",
            new AddressValue("İstanbul", "Kadıköy", "Moda Cad. No:10"),
            new EmergencyContactValue("Ayşe Yılmaz", "Eş", "05559876543"),
            new CommunicationPreferencesValue { AllowEmail = true, AllowSms = true },
            now);

        Assert.True(patient.IsActive);
        Assert.Equal("Can", patient.FirstName);
        Assert.NotNull(patient.PhoneNumber);

        // Execute KVKK anonymization
        patient.Anonymize(now.AddYears(10));

        // PII must be completely and irreversibly scrubbed
        Assert.False(patient.IsActive);
        Assert.Equal("ANONİM", patient.FirstName);
        Assert.Equal("HASTA", patient.LastName);
        Assert.Null(patient.NationalIdSynthetic);
        Assert.Null(patient.PhoneNumber);
        Assert.Null(patient.Email);
        Assert.Null(patient.Address);
        Assert.Null(patient.EmergencyContact);
        Assert.False(patient.CommunicationPreferences.AllowEmail);
        Assert.False(patient.CommunicationPreferences.AllowSms);

        // Relational continuity must remain intact to avoid corrupting foreign keys of past encounters/audit
        Assert.Equal(patientId, patient.Id);
        Assert.Equal(personId, patient.PersonId);
        Assert.Equal("MRN-2026-9999", patient.MedicalRecordNumber);
    }
}
