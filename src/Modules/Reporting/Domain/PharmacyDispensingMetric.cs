namespace HospitalManagement.Modules.Reporting.Domain;

public sealed class PharmacyDispensingMetric
{
    private PharmacyDispensingMetric()
    {
    }

    public PharmacyDispensingMetric(
        DateOnly date,
        int totalPrescriptions,
        int pendingDispenseCount,
        int dispensedCount,
        int lowStockItemCount,
        int nearExpiryLotCount,
        DateTime createdUtc)
    {
        Id = Guid.NewGuid();
        Date = date;
        TotalPrescriptions = Math.Max(0, totalPrescriptions);
        PendingDispenseCount = Math.Max(0, pendingDispenseCount);
        DispensedCount = Math.Max(0, dispensedCount);
        LowStockItemCount = Math.Max(0, lowStockItemCount);
        NearExpiryLotCount = Math.Max(0, nearExpiryLotCount);
        LastUpdatedUtc = createdUtc;
    }

    public Guid Id
    {
        get; private set;
    }
    public DateOnly Date
    {
        get; private set;
    }
    public int TotalPrescriptions
    {
        get; private set;
    }
    public int PendingDispenseCount
    {
        get; private set;
    }
    public int DispensedCount
    {
        get; private set;
    }
    public int LowStockItemCount
    {
        get; private set;
    }
    public int NearExpiryLotCount
    {
        get; private set;
    }
    public DateTime LastUpdatedUtc
    {
        get; private set;
    }

    public void UpdateCounts(
        int pendingDelta,
        int dispensedDelta,
        int? lowStockCount,
        int? nearExpiryCount,
        DateTime updatedUtc)
    {
        PendingDispenseCount = Math.Max(0, PendingDispenseCount + pendingDelta);
        DispensedCount = Math.Max(0, DispensedCount + dispensedDelta);

        if (pendingDelta > 0)
        {
            TotalPrescriptions += pendingDelta;
        }

        if (lowStockCount.HasValue)
        {
            LowStockItemCount = Math.Max(0, lowStockCount.Value);
        }

        if (nearExpiryCount.HasValue)
        {
            NearExpiryLotCount = Math.Max(0, nearExpiryCount.Value);
        }

        LastUpdatedUtc = updatedUtc;
    }
}
