using HospitalManagement.Modules.Pharmacy.Domain;

namespace HospitalManagement.UnitTests.Pharmacy;

public sealed class MedicationStockDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G06")]
    public void CreateStockItemWithValidDataSucceedsAndSetsInitialState()
    {
        var id = Guid.NewGuid();
        var medId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);
        var expUtc = nowUtc.AddMonths(12);

        var item = MedicationStockItem.Create(
            id,
            Guid.NewGuid(),
            "Merkez Eczane Deposu",
            medId,
            "LOT-2026-001",
            expUtc,
            initialQuantity: 100,
            reorderLevel: 20,
            nowUtc);

        Assert.Equal(id, item.Id);
        Assert.Equal("Merkez Eczane Deposu", item.Location);
        Assert.Equal(medId, item.MedicationCatalogItemId);
        Assert.Equal("LOT-2026-001", item.LotNumber);
        Assert.Equal(expUtc, item.ExpirationDateUtc);
        Assert.Equal(100, item.QuantityOnHand);
        Assert.Equal(0, item.QuantityReserved);
        Assert.Equal(100, item.QuantityAvailable);
        Assert.Equal(20, item.ReorderLevel);
        Assert.Equal(1, item.Version);
        Assert.False(item.IsExpired(nowUtc));
        Assert.False(item.IsLowStock);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(50, -5)]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G06")]
    public void CreateStockItemWithNegativeQuantitiesThrowsArgumentException(int initialQty, int reorderLevel)
    {
        var nowUtc = DateTime.UtcNow;
        Assert.Throws<ArgumentException>(() =>
            MedicationStockItem.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Depo",
                Guid.NewGuid(),
                "LOT-01",
                nowUtc.AddMonths(6),
                initialQty,
                reorderLevel,
                nowUtc));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G06")]
    public void DeductStockValidQuantityDecreasesQuantityOnHandAndIncrementsVersion()
    {
        var nowUtc = DateTime.UtcNow;
        var item = MedicationStockItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Depo",
            Guid.NewGuid(),
            "LOT-01",
            nowUtc.AddMonths(6),
            initialQuantity: 50,
            reorderLevel: 10,
            nowUtc);

        item.DeductStock(20, nowUtc.AddMinutes(5));

        Assert.Equal(30, item.QuantityOnHand);
        Assert.Equal(2, item.Version);
        Assert.Equal(nowUtc.AddMinutes(5), item.UpdatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G06")]
    public void DeductStockExceedingQuantityOnHandThrowsInvalidOperationException()
    {
        var nowUtc = DateTime.UtcNow;
        var item = MedicationStockItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Depo",
            Guid.NewGuid(),
            "LOT-01",
            nowUtc.AddMonths(6),
            initialQuantity: 15,
            reorderLevel: 5,
            nowUtc);

        var ex = Assert.Throws<InvalidOperationException>(() => item.DeductStock(20, nowUtc));
        Assert.Contains("Yetersiz kullanılabilir stok", ex.Message, StringComparison.Ordinal);
        Assert.Equal(15, item.QuantityOnHand);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G06")]
    public void DeductStockCannotConsumeReservedQuantity()
    {
        var nowUtc = DateTime.UtcNow;
        var item = MedicationStockItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Depo",
            Guid.NewGuid(),
            "LOT-RESERVED",
            nowUtc.AddMonths(6),
            initialQuantity: 20,
            reorderLevel: 5,
            nowUtc);

        item.ReserveStock(15, nowUtc);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            item.DeductStock(6, nowUtc.AddMinutes(1)));

        Assert.Contains("Yetersiz kullanılabilir stok", exception.Message, StringComparison.Ordinal);
        Assert.Equal(20, item.QuantityOnHand);
        Assert.Equal(15, item.QuantityReserved);
        Assert.Equal(5, item.QuantityAvailable);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G06")]
    public void ReserveAndReleaseStockOperatesCorrectly()
    {
        var nowUtc = DateTime.UtcNow;
        var item = MedicationStockItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Depo",
            Guid.NewGuid(),
            "LOT-01",
            nowUtc.AddMonths(6),
            initialQuantity: 50,
            reorderLevel: 10,
            nowUtc);

        item.ReserveStock(15, nowUtc);
        Assert.Equal(50, item.QuantityOnHand);
        Assert.Equal(15, item.QuantityReserved);
        Assert.Equal(35, item.QuantityAvailable);

        item.ReleaseReservation(10, nowUtc);
        Assert.Equal(5, item.QuantityReserved);
        Assert.Equal(45, item.QuantityAvailable);

        Assert.Throws<InvalidOperationException>(() => item.ReserveStock(50, nowUtc));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G06")]
    public void AdjustStockUpdatesQuantityAndClampsReservation()
    {
        var nowUtc = DateTime.UtcNow;
        var item = MedicationStockItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Depo",
            Guid.NewGuid(),
            "LOT-01",
            nowUtc.AddMonths(6),
            initialQuantity: 50,
            reorderLevel: 10,
            nowUtc);

        item.ReserveStock(20, nowUtc);
        Assert.Equal(20, item.QuantityReserved);

        item.AdjustStock(10, nowUtc.AddMinutes(10));
        Assert.Equal(10, item.QuantityOnHand);
        Assert.Equal(10, item.QuantityReserved);
        Assert.Equal(0, item.QuantityAvailable);
    }
}
