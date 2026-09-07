using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record CreateEncounterCommand(
    Guid? AppointmentId,
    Guid PatientId,
    Guid DepartmentId,
    Guid PrimaryPractitionerId,
    EncounterType EncounterType,
    DateTime? PlannedStartTimeUtc,
    string? ChiefComplaint,
    bool StartImmediately = false);

public sealed record StartEncounterCommand(
    Guid EncounterId,
    Guid PractitionerId,
    long ExpectedVersion,
    DateTime? StartTimeUtc = null);

public sealed record CompleteEncounterCommand(
    Guid EncounterId,
    Guid PractitionerId,
    long ExpectedVersion,
    DateTime? EndTimeUtc = null,
    string? Summary = null);

public sealed record ReopenEncounterCommand(
    Guid EncounterId,
    Guid PractitionerId,
    long ExpectedVersion,
    string Reason);

public sealed record CancelEncounterCommand(
    Guid EncounterId,
    Guid PractitionerId,
    long ExpectedVersion,
    string Reason);

public sealed record MarkEncounterEnteredInErrorCommand(
    Guid EncounterId,
    Guid PractitionerId,
    long ExpectedVersion,
    string Reason);

public sealed record AddEncounterParticipantCommand(
    Guid EncounterId,
    Guid PractitionerId,
    long ExpectedVersion,
    ParticipantRole Role);
