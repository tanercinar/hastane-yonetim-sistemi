namespace HospitalManagement.Contracts.Pharmacy;

public sealed record MedicationStockItemResponse(
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
    int Version = 0);

public sealed record FefoCandidateStockResponse(
    Guid StockItemId,
    string Location,
    string LotNumber,
    DateTime ExpirationDateUtc,
    int QuantityAvailable,
    int DaysUntilExpiration,
    int Version = 0);

public sealed record AdjustStockRequest
{
    public Guid StockItemId
    {
        get; init;
    }
    public int ExpectedVersion
    {
        get; init;
    }
    public int NewQuantity
    {
        get; init;
    }
    public string Reason { get; init; } = string.Empty;
}

public sealed record MedicationStockTransactionResponse(
    Guid Id,
    Guid StockItemId,
    string TransactionType,
    int Quantity,
    int PreviousQuantityOnHand,
    int NewQuantityOnHand,
    string? ReferenceId,
    string? Notes,
    DateTime PerformedAtUtc);
