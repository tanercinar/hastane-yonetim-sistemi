namespace HospitalManagement.Contracts.Diagnostics;

public sealed record LabCatalogParameterResponse(
    Guid Id,
    Guid LabCatalogItemId,
    string Code,
    string Name,
    string Unit,
    decimal? ReferenceRangeLow,
    decimal? ReferenceRangeHigh,
    decimal? CriticalLow,
    decimal? CriticalHigh,
    string ValueType,
    int SortOrder);

public sealed record LabCatalogItemResponse(
    Guid Id,
    string Code,
    string Name,
    string Category,
    string SpecimenType,
    string ContainerType,
    bool IsPanel,
    int TurnaroundMinutes,
    bool IsActive,
    string CatalogVersion,
    string? Description,
    DateTime CreatedAtUtc,
    IReadOnlyList<LabCatalogParameterResponse> Parameters);

public sealed record LabCatalogSummaryResponse(
    Guid Id,
    string Code,
    string Name,
    string Category,
    string SpecimenType,
    string ContainerType,
    bool IsPanel,
    int TurnaroundMinutes,
    bool IsActive,
    int ParameterCount);

public sealed record ImportLabCatalogParameterRequest
{
    public required string Code
    {
        get; init;
    }
    public required string Name
    {
        get; init;
    }
    public required string Unit
    {
        get; init;
    }
    public decimal? ReferenceRangeLow
    {
        get; init;
    }
    public decimal? ReferenceRangeHigh
    {
        get; init;
    }
    public decimal? CriticalLow
    {
        get; init;
    }
    public decimal? CriticalHigh
    {
        get; init;
    }
    public string ValueType { get; init; } = "Numeric";
    public int SortOrder
    {
        get; init;
    }
}

public sealed record ImportLabCatalogItemRequest
{
    public required string Code
    {
        get; init;
    }
    public required string Name
    {
        get; init;
    }
    public required string Category
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
    public bool IsPanel
    {
        get; init;
    }
    public int TurnaroundMinutes { get; init; } = 60;
    public string? Description
    {
        get; init;
    }
    public List<ImportLabCatalogParameterRequest> Parameters { get; init; } = [];
}

public sealed record ImportLabCatalogRequest
{
    public required string CatalogVersion
    {
        get; init;
    }
    public List<ImportLabCatalogItemRequest> Items { get; init; } = [];
}

public sealed record ImportLabCatalogResponse(
    string CatalogVersion,
    int TotalProcessed,
    int TotalAdded,
    int TotalUpdated,
    DateTime ImportedAtUtc);
