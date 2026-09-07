namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record EncounterParticipantResponse(
    Guid Id,
    Guid EncounterId,
    Guid PractitionerId,
    string Role,
    DateTime JoinedAtUtc,
    DateTime? LeftAtUtc);

public sealed record EncounterDetailResponse(
    Guid Id,
    Guid? AppointmentId,
    Guid PatientId,
    Guid DepartmentId,
    Guid PrimaryPractitionerId,
    string EncounterType,
    string Status,
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
    IReadOnlyList<EncounterParticipantResponse> Participants);

public sealed record EncounterSummaryResponse(
    Guid Id,
    Guid? AppointmentId,
    Guid PatientId,
    Guid DepartmentId,
    Guid PrimaryPractitionerId,
    string EncounterType,
    string Status,
    DateTime? StartTimeUtc,
    string? ChiefComplaint,
    DateTime CreatedAtUtc);
