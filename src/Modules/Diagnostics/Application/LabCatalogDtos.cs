namespace HospitalManagement.Modules.Diagnostics.Application;

public sealed record LabCatalogParameterDto(
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

public sealed record LabCatalogItemDto(
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
    IReadOnlyList<LabCatalogParameterDto> Parameters);

public sealed record LabCatalogSummaryDto(
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

public sealed record ImportLabCatalogParameterCommand(
    string Code,
    string Name,
    string Unit,
    decimal? ReferenceRangeLow,
    decimal? ReferenceRangeHigh,
    decimal? CriticalLow,
    decimal? CriticalHigh,
    string ValueType,
    int SortOrder);

public sealed record ImportLabCatalogItemCommand(
    string Code,
    string Name,
    string Category,
    string SpecimenType,
    string ContainerType,
    bool IsPanel,
    int TurnaroundMinutes,
    string? Description,
    IReadOnlyList<ImportLabCatalogParameterCommand> Parameters);

public sealed record ImportLabCatalogCommand(
    string CatalogVersion,
    IReadOnlyList<ImportLabCatalogItemCommand> Items);

public sealed record ImportLabCatalogResultDto(
    string CatalogVersion,
    int TotalProcessed,
    int TotalAdded,
    int TotalUpdated,
    DateTime ImportedAtUtc);
