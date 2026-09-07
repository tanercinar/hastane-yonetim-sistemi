namespace HospitalManagement.Modules.Emergency.Domain;

public sealed record EmergencyDispositionInfo(
    EmergencyDispositionType DispositionType,
    Guid DecidedByDoctorId,
    DateTime DecidedAtUtc,
    Guid? TargetWardOrIcuId,
    string? TargetDepartmentName,
    string DispositionSummaryNotes,
    string? FollowUpInstructions);
