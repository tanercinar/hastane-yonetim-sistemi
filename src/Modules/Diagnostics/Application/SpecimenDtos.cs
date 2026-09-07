using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public sealed record SpecimenTransitionEventDto(
    Guid Id,
    Guid SpecimenId,
    SpecimenStatus FromStatus,
    SpecimenStatus ToStatus,
    DateTime TransitionedAtUtc,
    Guid ActorUserId,
    string ActorRole,
    string? Location,
    string? Notes);

public sealed record SpecimenDetailDto(
    Guid Id,
    string Barcode,
    Guid DiagnosticOrderId,
    Guid PatientId,
    string SpecimenType,
    string ContainerType,
    SpecimenStatus Status,
    string? CollectionNotes,
    string? RejectionReason,
    DateTime? CollectedAtUtc,
    Guid? CollectedByUserId,
    DateTime? ReceivedAtUtc,
    Guid? ReceivedByUserId,
    DateTime? RejectedAtUtc,
    Guid? RejectedByUserId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    int Version,
    IReadOnlyList<SpecimenTransitionEventDto> Transitions);

public sealed record SpecimenSummaryDto(
    Guid Id,
    string Barcode,
    Guid DiagnosticOrderId,
    Guid PatientId,
    string SpecimenType,
    string ContainerType,
    SpecimenStatus Status,
    DateTime? CollectedAtUtc,
    DateTime? ReceivedAtUtc,
    DateTime CreatedAtUtc);

public sealed record CollectSpecimenCommand(
    Guid DiagnosticOrderId,
    Guid PatientId,
    string SpecimenType,
    string ContainerType,
    string? CollectionLocation,
    string? CollectionNotes);

public sealed record TransitSpecimenCommand(
    string? Location,
    string? Notes);

public sealed record ReceiveSpecimenCommand(
    string? Location,
    string? Notes);

public sealed record RejectSpecimenCommand(
    string RejectionReason,
    string? Location,
    string? Notes);
