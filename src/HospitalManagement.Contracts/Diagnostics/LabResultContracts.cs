namespace HospitalManagement.Contracts.Diagnostics;

public sealed record LabResultItemValueRequest
{
    public required string ParameterCode
    {
        get; init;
    }
    public decimal? NumericValue
    {
        get; init;
    }
    public string? StringValue
    {
        get; init;
    }
    public string? Notes
    {
        get; init;
    }
}

public sealed record CreateDraftLabResultRequest
{
    public required Guid DiagnosticOrderId
    {
        get; init;
    }
    public required Guid DiagnosticOrderItemId
    {
        get; init;
    }
    public Guid? SpecimenId
    {
        get; init;
    }
    public required Guid PatientId
    {
        get; init;
    }
    public required string CatalogCode
    {
        get; init;
    }
    public required string CatalogItemName
    {
        get; init;
    }
    public string? ClinicalNotes
    {
        get; init;
    }
    public List<LabResultItemValueRequest>? Items
    {
        get; init;
    }
}

public sealed record UpdateLabResultItemsRequest
{
    public required List<LabResultItemValueRequest> Items
    {
        get; init;
    }
    public string? ClinicalNotes
    {
        get; init;
    }
}

public sealed record CorrectLabResultRequest
{
    public required string CorrectionReason
    {
        get; init;
    }
    public required List<LabResultItemValueRequest> CorrectedItems
    {
        get; init;
    }
}

public sealed record LabResultItemResponse(
    Guid Id,
    Guid LabResultId,
    string ParameterCode,
    string ParameterName,
    decimal? NumericValue,
    string? StringValue,
    string? Unit,
    decimal? ReferenceRangeLow,
    decimal? ReferenceRangeHigh,
    string? ReferenceRangeText,
    string Flag,
    string? Notes);

public sealed record LabResultDetailResponse(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid DiagnosticOrderItemId,
    Guid? SpecimenId,
    Guid PatientId,
    string CatalogCode,
    string CatalogItemName,
    string Status,
    Guid? TechnicallyApprovedByUserId,
    DateTime? TechnicallyApprovedAtUtc,
    Guid? ClinicallyApprovedByUserId,
    DateTime? ClinicallyApprovedAtUtc,
    Guid? PreviousResultId,
    string? CorrectionReason,
    string? ClinicalNotes,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    int Version,
    IReadOnlyList<LabResultItemResponse> Items);

public sealed record LabResultSummaryResponse(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid DiagnosticOrderItemId,
    Guid PatientId,
    string CatalogCode,
    string CatalogItemName,
    string Status,
    bool HasCriticalFlag,
    bool HasAbnormalFlag,
    DateTime CreatedAtUtc,
    DateTime? ClinicallyApprovedAtUtc);
