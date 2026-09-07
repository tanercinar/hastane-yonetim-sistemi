using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record EncounterParticipantDto(
    Guid Id,
    Guid EncounterId,
    Guid PractitionerId,
    ParticipantRole Role,
    DateTime JoinedAtUtc,
    DateTime? LeftAtUtc);

public sealed record EncounterDto(
    Guid Id,
    Guid? AppointmentId,
    Guid PatientId,
    Guid DepartmentId,
    Guid PrimaryPractitionerId,
    EncounterType EncounterType,
    EncounterStatus Status,
    DateTime? PlannedStartTimeUtc,
    DateTime? ActualStartTimeUtc,
    DateTime? ActualEndTimeUtc,
    string? ChiefComplaint,
    string? CancellationReason,
    string? EnteredInErrorReason,
    string? ReopenReason,
    DateTime? ReopenedAtUtc,
    Guid? ReopenedByPractitionerId,
    long Version,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<EncounterParticipantDto> Participants);
