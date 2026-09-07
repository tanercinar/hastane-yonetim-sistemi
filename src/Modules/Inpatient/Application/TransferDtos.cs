using HospitalManagement.Modules.Inpatient.Domain;

namespace HospitalManagement.Modules.Inpatient.Application;

public sealed record InpatientTransferDto(
    Guid Id,
    Guid AdmissionId,
    Guid PatientId,
    Guid SourceWardId,
    string SourceWardName,
    Guid SourceBedId,
    string SourceBedNumber,
    string? SourceRoomNumber,
    Guid TargetWardId,
    string TargetWardName,
    Guid? TargetBedId,
    string? TargetBedNumber,
    string? TargetRoomNumber,
    string TransferReason,
    string? ClinicalNotes,
    TransferStatus Status,
    Guid RequestedByUserId,
    DateTime RequestedAtUtc,
    Guid? AcceptedByUserId,
    DateTime? AcceptedAtUtc,
    Guid? CompletedByUserId,
    DateTime? CompletedAtUtc,
    Guid? CancelledByUserId,
    DateTime? CancelledAtUtc,
    string? CancellationReason,
    int Version);

public sealed record InpatientTransferSummaryDto(
    Guid Id,
    Guid AdmissionId,
    Guid PatientId,
    Guid SourceWardId,
    string SourceWardName,
    Guid SourceBedId,
    string SourceBedNumber,
    Guid TargetWardId,
    string TargetWardName,
    Guid? TargetBedId,
    string? TargetBedNumber,
    string TransferReason,
    TransferStatus Status,
    DateTime RequestedAtUtc,
    DateTime? CompletedAtUtc);

public sealed record CreateTransferDto(
    Guid AdmissionId,
    Guid TargetWardId,
    Guid? TargetBedId,
    string TransferReason,
    string? ClinicalNotes);

public sealed record AcceptTransferDto(
    Guid? TargetBedId);

public sealed record CompleteTransferDto(
    Guid TargetBedId);

public sealed record CancelTransferDto(
    string Reason);
