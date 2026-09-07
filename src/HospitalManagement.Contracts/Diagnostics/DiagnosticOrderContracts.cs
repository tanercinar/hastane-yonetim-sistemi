namespace HospitalManagement.Contracts.Diagnostics;

public sealed record CreateDiagnosticOrderItemRequest
{
    public required string CatalogCode
    {
        get; init;
    }
    public required string CatalogItemName
    {
        get; init;
    }
    public string Category { get; init; } = "General";
    public string? SpecialInstructions
    {
        get; init;
    }
}

public sealed record CreateDiagnosticOrderDraftRequest
{
    public required Guid PatientId
    {
        get; init;
    }
    public required Guid EncounterId
    {
        get; init;
    }
    public required Guid DepartmentId
    {
        get; init;
    }
    public string OrderType { get; init; } = "Laboratory";
    public string Priority { get; init; } = "Routine";
    public string? ClinicalIndication
    {
        get; init;
    }
    public string? OrderNotes
    {
        get; init;
    }
    public List<CreateDiagnosticOrderItemRequest> Items { get; init; } = [];
}

public sealed record UpdateDiagnosticOrderDraftRequest
{
    public string? Priority
    {
        get; init;
    }
    public string? ClinicalIndication
    {
        get; init;
    }
    public string? OrderNotes
    {
        get; init;
    }
    public List<CreateDiagnosticOrderItemRequest> Items { get; init; } = [];
}

public sealed record PlaceDiagnosticOrderRequest
{
    public string? Notes
    {
        get; init;
    }
}

public sealed record CancelDiagnosticOrderRequest
{
    public required string Reason
    {
        get; init;
    }
}

public sealed record MarkDiagnosticOrderEnteredInErrorRequest
{
    public required string Reason
    {
        get; init;
    }
}

public sealed record DiagnosticOrderItemResponse(
    Guid Id,
    Guid DiagnosticOrderId,
    string CatalogCode,
    string CatalogItemName,
    string Category,
    string Status,
    string? SpecialInstructions,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record DiagnosticOrderDetailResponse(
    Guid Id,
    string OrderNumber,
    Guid PatientId,
    Guid EncounterId,
    Guid PlacingDoctorId,
    Guid DepartmentId,
    string OrderType,
    string Priority,
    string Status,
    string? ClinicalIndication,
    string? OrderNotes,
    string? CancellationReason,
    string? EnteredInErrorReason,
    DateTime? PlacedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? CancelledAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    uint Version,
    IReadOnlyList<DiagnosticOrderItemResponse> Items);

public sealed record DiagnosticOrderSummaryResponse(
    Guid Id,
    string OrderNumber,
    Guid PatientId,
    Guid EncounterId,
    Guid PlacingDoctorId,
    Guid DepartmentId,
    string OrderType,
    string Priority,
    string Status,
    int ItemCount,
    DateTime? PlacedAtUtc,
    DateTime CreatedAtUtc);
