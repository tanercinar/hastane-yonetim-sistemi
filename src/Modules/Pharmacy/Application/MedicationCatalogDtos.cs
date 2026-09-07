using HospitalManagement.Modules.Pharmacy.Domain;

namespace HospitalManagement.Modules.Pharmacy.Application;

public sealed record MedicationCatalogItemDto(
    Guid Id,
    string Code,
    string BrandName,
    string GenericName,
    MedicationForm Form,
    decimal StrengthValue,
    string StrengthUnit,
    MedicationRoute Route,
    string? AtcCode,
    string? Description,
    string CatalogVersion,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record MedicationCatalogItemImportDto(
    string Code,
    string BrandName,
    string GenericName,
    MedicationForm Form,
    decimal StrengthValue,
    string StrengthUnit,
    MedicationRoute Route,
    string? AtcCode,
    string? Description);

public sealed record MedicationCatalogImportResultDto(
    string CatalogVersion,
    int TotalItems,
    int InsertedCount,
    int UpdatedCount);
