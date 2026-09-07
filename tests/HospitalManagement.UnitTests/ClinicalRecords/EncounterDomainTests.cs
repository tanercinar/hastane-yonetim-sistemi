using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.UnitTests.ClinicalRecords;

public sealed class EncounterDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G01")]
    public void CreateEncounterInitializesPlannedStatusAndPrimaryParticipant()
    {
        var encounterId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var encounter = Encounter.Create(
            encounterId,
            appointmentId,
            patientId,
            departmentId,
            doctorId,
            EncounterType.Outpatient,
            nowUtc.AddMinutes(15),
            "Baş ağrısı ve halsizlik",
            nowUtc);

        Assert.Equal(encounterId, encounter.Id);
        Assert.Equal(appointmentId, encounter.AppointmentId);
        Assert.Equal(patientId, encounter.PatientId);
        Assert.Equal(departmentId, encounter.DepartmentId);
        Assert.Equal(doctorId, encounter.PrimaryPractitionerId);
        Assert.Equal(EncounterType.Outpatient, encounter.EncounterType);
        Assert.Equal(EncounterStatus.Planned, encounter.Status);
        Assert.Equal("Baş ağrısı ve halsizlik", encounter.ChiefComplaint);
        Assert.Equal(1, encounter.Version);
        Assert.Null(encounter.ActualStartTimeUtc);
        Assert.Null(encounter.ActualEndTimeUtc);

        Assert.Single(encounter.Participants);
        var primary = encounter.Participants.First();
        Assert.Equal(doctorId, primary.PractitionerId);
        Assert.Equal(ParticipantRole.PrimaryAttending, primary.Role);
        Assert.Null(primary.LeftAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G01")]
    public void StartTransitionChangesStatusToInProgress()
    {
        var encounter = CreateSampleEncounter();
        var startUtc = DateTime.UtcNow;

        encounter.Start(encounter.PrimaryPractitionerId, startUtc);

        Assert.Equal(EncounterStatus.InProgress, encounter.Status);
        Assert.Equal(startUtc, encounter.ActualStartTimeUtc);
        Assert.Equal(2, encounter.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G01")]
    public void StartOnAlreadyInProgressOrCompletedEncounterThrowsInvalidOperationException()
    {
        var encounter = CreateSampleEncounter();
        encounter.Start(encounter.PrimaryPractitionerId, DateTime.UtcNow);

        // Attempt second start
        Assert.Throws<InvalidOperationException>(() =>
            encounter.Start(encounter.PrimaryPractitionerId, DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G01")]
    public void CompleteTransitionChangesStatusToCompleted()
    {
        var encounter = CreateSampleEncounter();
        encounter.Start(encounter.PrimaryPractitionerId, DateTime.UtcNow);

        var endUtc = DateTime.UtcNow.AddMinutes(20);
        encounter.Complete(encounter.PrimaryPractitionerId, endUtc);

        Assert.Equal(EncounterStatus.Completed, encounter.Status);
        Assert.Equal(endUtc, encounter.ActualEndTimeUtc);
        Assert.Equal(3, encounter.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G01")]
    public void CompleteWithoutStartingThrowsInvalidOperationException()
    {
        var encounter = CreateSampleEncounter();

        Assert.Throws<InvalidOperationException>(() =>
            encounter.Complete(encounter.PrimaryPractitionerId, DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G01")]
    public void CancelEncounterSetsStatusAndReason()
    {
        var encounter = CreateSampleEncounter();

        encounter.Cancel(encounter.PrimaryPractitionerId, "Hasta gelmedi", DateTime.UtcNow);

        Assert.Equal(EncounterStatus.Cancelled, encounter.Status);
        Assert.Equal("Hasta gelmedi", encounter.CancellationReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G01")]
    public void CancelOnCompletedEncounterThrowsInvalidOperationException()
    {
        var encounter = CreateSampleEncounter();
        encounter.Start(encounter.PrimaryPractitionerId, DateTime.UtcNow);
        encounter.Complete(encounter.PrimaryPractitionerId, DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            encounter.Cancel(encounter.PrimaryPractitionerId, "İptal denemesi", DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G01")]
    public void MarkEnteredInErrorSetsStatusAndReason()
    {
        var encounter = CreateSampleEncounter();

        encounter.MarkEnteredInError(encounter.PrimaryPractitionerId, "Yanlış hasta seçildi", DateTime.UtcNow);

        Assert.Equal(EncounterStatus.EnteredInError, encounter.Status);
        Assert.Equal("Yanlış hasta seçildi", encounter.EnteredInErrorReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G01")]
    public void AddAndRemoveParticipantsUpdatesCollectionCorrectly()
    {
        var encounter = CreateSampleEncounter();
        var nurseId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        encounter.AddParticipant(nurseId, ParticipantRole.AssistingNurse, nowUtc);
        Assert.Equal(2, encounter.Participants.Count);

        var nurseParticipant = encounter.Participants.First(p => p.PractitionerId == nurseId);
        Assert.Equal(ParticipantRole.AssistingNurse, nurseParticipant.Role);
        Assert.Null(nurseParticipant.LeftAtUtc);

        var leaveUtc = nowUtc.AddMinutes(30);
        encounter.RemoveParticipant(nurseId, leaveUtc);

        Assert.NotNull(nurseParticipant.LeftAtUtc);
        Assert.Equal(leaveUtc, nurseParticipant.LeftAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G01")]
    public void EmptyIdsThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Encounter.Create(
                Guid.Empty,
                null,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                EncounterType.Outpatient,
                null,
                null,
                DateTime.UtcNow));

        Assert.Throws<ArgumentException>(() =>
            Encounter.Create(
                Guid.NewGuid(),
                null,
                Guid.Empty,
                Guid.NewGuid(),
                Guid.NewGuid(),
                EncounterType.Outpatient,
                null,
                null,
                DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G09")]
    public void ReopenCompletedEncounterTransitionsToInProgressAndRecordsReason()
    {
        var encounter = CreateSampleEncounter();
        var docId = encounter.PrimaryPractitionerId;
        var startUtc = DateTime.UtcNow;
        encounter.Start(docId, startUtc);

        var endUtc = startUtc.AddMinutes(30);
        encounter.Complete(docId, endUtc);
        Assert.Equal(EncounterStatus.Completed, encounter.Status);

        var reopenUtc = endUtc.AddMinutes(15);
        encounter.Reopen(docId, "Ek klinik değerlendirme ve reçete düzeltmesi", reopenUtc);

        Assert.Equal(EncounterStatus.InProgress, encounter.Status);
        Assert.Equal("Ek klinik değerlendirme ve reçete düzeltmesi", encounter.ReopenReason);
        Assert.Equal(reopenUtc, encounter.ReopenedAtUtc);
        Assert.Equal(docId, encounter.ReopenedByPractitionerId);
        Assert.Null(encounter.ActualEndTimeUtc);
        Assert.Equal(4, encounter.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G09")]
    public void ReopenOnNonCompletedEncounterThrowsInvalidOperationException()
    {
        var encounter = CreateSampleEncounter();
        Assert.Throws<InvalidOperationException>(() =>
            encounter.Reopen(encounter.PrimaryPractitionerId, "Gerekçe", DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G09")]
    public void ReopenWithEmptyReasonThrowsArgumentException()
    {
        var encounter = CreateSampleEncounter();
        encounter.Start(encounter.PrimaryPractitionerId, DateTime.UtcNow);
        encounter.Complete(encounter.PrimaryPractitionerId, DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() =>
            encounter.Reopen(encounter.PrimaryPractitionerId, "   ", DateTime.UtcNow));
    }

    private static Encounter CreateSampleEncounter()
    {
        return Encounter.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            EncounterType.Outpatient,
            DateTime.UtcNow,
            "Kontrol muayenesi",
            DateTime.UtcNow);
    }
}
