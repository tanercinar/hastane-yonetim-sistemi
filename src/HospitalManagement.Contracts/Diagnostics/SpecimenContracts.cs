namespace HospitalManagement.Contracts.Diagnostics;

public sealed record CollectSpecimenRequest
{
    public required Guid DiagnosticOrderId
    {
        get; init;
    }
    public required Guid PatientId
    {
        get; init;
    }
    public required string SpecimenType
    {
        get; init;
    }
    public required string ContainerType
    {
        get; init;
    }
    public string? CollectionLocation
    {
        get; init;
    }
    public string? CollectionNotes
    {
        get; init;
    }
}

public sealed record TransitSpecimenRequest
{
    public string? Location
    {
        get; init;
    }
    public string? Notes
    {
        get; init;
    }
}

public sealed record ReceiveSpecimenRequest
{
    public string? Location
    {
        get; init;
    }
    public string? Notes
    {
        get; init;
    }
}

public sealed record RejectSpecimenRequest
{
    public required string RejectionReason
    {
        get; init;
    }
    public string? Location
    {
        get; init;
    }
    public string? Notes
    {
        get; init;
    }
}

public sealed record SpecimenTransitionEventResponse(
    Guid Id,
    Guid SpecimenId,
    string FromStatus,
    string ToStatus,
    DateTime TransitionedAtUtc,
    Guid ActorUserId,
    string ActorRole,
    string? Location,
    string? Notes);

public sealed record SpecimenDetailResponse(
    Guid Id,
    string Barcode,
    Guid DiagnosticOrderId,
    Guid PatientId,
    string SpecimenType,
    string ContainerType,
    string Status,
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
    IReadOnlyList<SpecimenTransitionEventResponse> Transitions);

public sealed record SpecimenSummaryResponse(
    Guid Id,
    string Barcode,
    Guid DiagnosticOrderId,
    Guid PatientId,
    string SpecimenType,
    string ContainerType,
    string Status,
    DateTime? CollectedAtUtc,
    DateTime? ReceivedAtUtc,
    DateTime CreatedAtUtc);
