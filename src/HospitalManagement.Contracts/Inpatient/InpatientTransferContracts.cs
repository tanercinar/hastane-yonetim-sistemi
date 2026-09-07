namespace HospitalManagement.Contracts.Inpatient;

public sealed record TransferResponse(
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
    string Status,
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

public sealed record TransferSummaryResponse(
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
    string Status,
    DateTime RequestedAtUtc,
    DateTime? CompletedAtUtc);

public sealed record CreateTransferRequest
{
    public Guid AdmissionId
    {
        get; init;
    }
    public Guid TargetWardId
    {
        get; init;
    }
    public Guid? TargetBedId
    {
        get; init;
    }
    public string TransferReason { get; init; } = string.Empty;
    public string? ClinicalNotes
    {
        get; init;
    }
}

public sealed record AcceptTransferRequest
{
    public Guid? TargetBedId
    {
        get; init;
    }
}

public sealed record CompleteTransferRequest
{
    public Guid TargetBedId
    {
        get; init;
    }
}

public sealed record CancelTransferRequest
{
    public string Reason { get; init; } = string.Empty;
}
