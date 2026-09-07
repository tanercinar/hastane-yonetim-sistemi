namespace HospitalManagement.Contracts.Pharmacy;

public sealed record MedicationCatalogItemResponse(
    Guid Id,
    string Code,
    string BrandName,
    string GenericName,
    string Form,
    decimal StrengthValue,
    string StrengthUnit,
    string Route,
    string? AtcCode,
    string? Description,
    string CatalogVersion,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record MedicationCatalogImportResultResponse(
    string CatalogVersion,
    int TotalItems,
    int InsertedCount,
    int UpdatedCount);
