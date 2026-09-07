using HospitalManagement.Modules.Pharmacy.Domain;

namespace HospitalManagement.Modules.Pharmacy.Application;

public sealed record MedicationStockItemDto(
    Guid Id,
    Guid DepartmentId,
    string Location,
    Guid MedicationCatalogItemId,
    string MedicationCode,
    string BrandName,
    string GenericName,
    string LotNumber,
    DateTime ExpirationDateUtc,
    int QuantityOnHand,
    int QuantityReserved,
    int QuantityAvailable,
    int ReorderLevel,
    bool IsExpired,
    bool IsLowStock,
    DateTime CreatedAtUtc,
    int Version);

public sealed record FefoCandidateDto(
    Guid StockItemId,
    string Location,
    string LotNumber,
    DateTime ExpirationDateUtc,
    int QuantityAvailable,
    int DaysUntilExpiration,
    int Version);

public sealed record MedicationStockTransactionDto(
    Guid Id,
    Guid StockItemId,
    StockTransactionType TransactionType,
    int Quantity,
    int PreviousQuantityOnHand,
    int NewQuantityOnHand,
    string? ReferenceId,
    string? Notes,
    DateTime PerformedAtUtc);

public sealed record AdjustStockCommand(
    Guid StockItemId,
    int ExpectedVersion,
    int NewQuantity,
    string Reason);
