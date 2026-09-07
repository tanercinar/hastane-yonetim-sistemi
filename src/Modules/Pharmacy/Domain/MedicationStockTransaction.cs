namespace HospitalManagement.Modules.Pharmacy.Domain;

public sealed class MedicationStockTransaction
{
    private MedicationStockTransaction()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public Guid StockItemId
    {
        get; private set;
    }
    public StockTransactionType TransactionType
    {
        get; private set;
    }
    public int Quantity
    {
        get; private set;
    }
    public int PreviousQuantityOnHand
    {
        get; private set;
    }
    public int NewQuantityOnHand
    {
        get; private set;
    }
    public string? ReferenceId
    {
        get; private set;
    }
    public string? Notes
    {
        get; private set;
    }
    public Guid? PerformedByUserId
    {
        get; private set;
    }
    public DateTime PerformedAtUtc
    {
        get; private set;
    }

    public static MedicationStockTransaction Create(
        Guid id,
        Guid stockItemId,
        StockTransactionType transactionType,
        int quantity,
        int previousQuantityOnHand,
        int newQuantityOnHand,
        string? referenceId,
        string? notes,
        Guid? performedByUserId,
        DateTime performedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Hareket kimliği boş olamaz.", nameof(id));
        }

        if (stockItemId == Guid.Empty)
        {
            throw new ArgumentException("Stok kalem kimliği boş olamaz.", nameof(stockItemId));
        }

        return new MedicationStockTransaction
        {
            Id = id,
            StockItemId = stockItemId,
            TransactionType = transactionType,
            Quantity = quantity,
            PreviousQuantityOnHand = previousQuantityOnHand,
            NewQuantityOnHand = newQuantityOnHand,
            ReferenceId = referenceId?.Trim(),
            Notes = notes?.Trim(),
            PerformedByUserId = performedByUserId,
            PerformedAtUtc = DateTime.SpecifyKind(performedAtUtc, DateTimeKind.Utc),
        };
    }
}
