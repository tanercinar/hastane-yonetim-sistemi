namespace HospitalManagement.Modules.Pharmacy.Domain;

public sealed class MedicationStockItem
{
    private MedicationStockItem()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public string Location { get; private set; } = string.Empty;
    public Guid DepartmentId
    {
        get; private set;
    }
    public Guid MedicationCatalogItemId
    {
        get; private set;
    }
    public string LotNumber { get; private set; } = string.Empty;
    public DateTime ExpirationDateUtc
    {
        get; private set;
    }
    public int QuantityOnHand
    {
        get; private set;
    }
    public int QuantityReserved
    {
        get; private set;
    }
    public int ReorderLevel
    {
        get; private set;
    }
    public int Version
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }
    public DateTime? UpdatedAtUtc
    {
        get; private set;
    }

    public int QuantityAvailable => Math.Max(0, QuantityOnHand - QuantityReserved);
    public bool IsExpired(DateTime nowUtc) => ExpirationDateUtc <= nowUtc;
    public bool IsLowStock => QuantityOnHand <= ReorderLevel;

    public static MedicationStockItem Create(
        Guid id,
        Guid departmentId,
        string location,
        Guid medicationCatalogItemId,
        string lotNumber,
        DateTime expirationDateUtc,
        int initialQuantity,
        int reorderLevel,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Stok kalem kimliği boş olamaz.", nameof(id));
        }

        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Stok bölümü kimliği boş olamaz.", nameof(departmentId));
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Depo/lokasyon bilgisi zorunludur.", nameof(location));
        }

        if (medicationCatalogItemId == Guid.Empty)
        {
            throw new ArgumentException("İlaç katalog kimliği boş olamaz.", nameof(medicationCatalogItemId));
        }

        if (string.IsNullOrWhiteSpace(lotNumber))
        {
            throw new ArgumentException("Parti/Lot numarası zorunludur.", nameof(lotNumber));
        }

        if (initialQuantity < 0)
        {
            throw new ArgumentException("Başlangıç stok miktarı negatif olamaz.", nameof(initialQuantity));
        }

        if (reorderLevel < 0)
        {
            throw new ArgumentException("Kritik stok seviyesi negatif olamaz.", nameof(reorderLevel));
        }

        return new MedicationStockItem
        {
            Id = id,
            DepartmentId = departmentId,
            Location = location.Trim(),
            MedicationCatalogItemId = medicationCatalogItemId,
            LotNumber = lotNumber.Trim().ToUpperInvariant(),
            ExpirationDateUtc = DateTime.SpecifyKind(expirationDateUtc, DateTimeKind.Utc),
            QuantityOnHand = initialQuantity,
            QuantityReserved = 0,
            ReorderLevel = reorderLevel,
            Version = 1,
            CreatedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc),
            UpdatedAtUtc = null,
        };
    }

    public void DeductStock(int quantity, DateTime nowUtc)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Düşülecek miktar 0'dan büyük olmalıdır.", nameof(quantity));
        }

        if (QuantityAvailable < quantity)
        {
            throw new InvalidOperationException(
                $"Yetersiz kullanılabilir stok. Kullanılabilir miktar: {QuantityAvailable}, İstenen: {quantity}. Lot: {LotNumber}");
        }

        QuantityOnHand -= quantity;
        Version++;
        UpdatedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
    }

    public void AddStock(int quantity, DateTime nowUtc)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Eklenecek miktar 0'dan büyük olmalıdır.", nameof(quantity));
        }

        QuantityOnHand += quantity;
        Version++;
        UpdatedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
    }

    public void ReserveStock(int quantity, DateTime nowUtc)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Rezerve edilecek miktar 0'dan büyük olmalıdır.", nameof(quantity));
        }

        if (QuantityAvailable < quantity)
        {
            throw new InvalidOperationException(
                $"Yetersiz kullanılabilir stok. Kullanılabilir miktar: {QuantityAvailable}, İstenen rezervasyon: {quantity}. Lot: {LotNumber}");
        }

        QuantityReserved += quantity;
        Version++;
        UpdatedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
    }

    public void ReleaseReservation(int quantity, DateTime nowUtc)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Serbest bırakılacak miktar 0'dan büyük olmalıdır.", nameof(quantity));
        }

        if (QuantityReserved < quantity)
        {
            throw new InvalidOperationException(
                $"Serbest bırakılacak miktar rezerve miktardan fazla olamaz. Rezerve miktar: {QuantityReserved}, İstenen: {quantity}. Lot: {LotNumber}");
        }

        QuantityReserved -= quantity;
        Version++;
        UpdatedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
    }

    public void AdjustStock(int newQuantity, DateTime nowUtc)
    {
        if (newQuantity < 0)
        {
            throw new ArgumentException("Düzeltilen stok miktarı negatif olamaz.", nameof(newQuantity));
        }

        QuantityOnHand = newQuantity;
        if (QuantityReserved > QuantityOnHand)
        {
            QuantityReserved = QuantityOnHand;
        }

        Version++;
        UpdatedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
    }
}
