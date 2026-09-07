using HospitalManagement.Modules.SurgeryCriticalCare.Domain;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Application;

public sealed record SavePerioperativeRecordDto(
    Guid SurgeryBookingId,
    DateTime? RoomEntryTimeUtc,
    DateTime? AnesthesiaStartTimeUtc,
    DateTime? IncisionTimeUtc,
    DateTime? ClosureTimeUtc,
    DateTime? AnesthesiaEndTimeUtc,
    DateTime? RoomExitTimeUtc,
    AnesthesiaType AnesthesiaType,
    string? AnesthesiaNotes,
    string? IntraoperativeFindings,
    string? IntraoperativeComplications,
    int? EstimatedBloodLossMl,
    string? SpecimensCollected,
    bool CountsConfirmed,
    PostOpDisposition PostOpDisposition,
    string? PostOpInstructions);

public sealed record AddPerioperativeCorrectionDto(
    string ReasonForCorrection,
    string CorrectionNote);

public sealed record PerioperativeCorrectionDto(
    Guid Id,
    Guid PerioperativeRecordId,
    Guid CorrectedByDoctorId,
    DateTime CorrectedAtUtc,
    string ReasonForCorrection,
    string CorrectionNote);

public sealed record PerioperativeRecordDto(
    Guid Id,
    Guid SurgeryBookingId,
    Guid PatientId,
    Guid OperatingRoomId,
    DateTime? RoomEntryTimeUtc,
    DateTime? AnesthesiaStartTimeUtc,
    DateTime? IncisionTimeUtc,
    DateTime? ClosureTimeUtc,
    DateTime? AnesthesiaEndTimeUtc,
    DateTime? RoomExitTimeUtc,
    AnesthesiaType AnesthesiaType,
    string? AnesthesiaNotes,
    string? IntraoperativeFindings,
    string? IntraoperativeComplications,
    int? EstimatedBloodLossMl,
    string? SpecimensCollected,
    bool CountsConfirmed,
    PostOpDisposition PostOpDisposition,
    string? PostOpInstructions,
    bool IsSigned,
    Guid? SignedByDoctorId,
    DateTime? SignedAtUtc,
    List<PerioperativeCorrectionDto> Corrections,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint Version);
