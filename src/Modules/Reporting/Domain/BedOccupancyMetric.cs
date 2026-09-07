namespace HospitalManagement.Modules.Reporting.Domain;

public sealed class BedOccupancyMetric
{
    private BedOccupancyMetric()
    {
    }

    public BedOccupancyMetric(
        DateOnly date,
        Guid departmentId,
        string departmentName,
        string wardType,
        int totalBeds,
        int occupiedBeds,
        int pendingTransfers,
        DateTime createdUtc)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Bölüm kimliği boş olamaz.", nameof(departmentId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(departmentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(wardType);

        Id = Guid.NewGuid();
        Date = date;
        DepartmentId = departmentId;
        DepartmentName = departmentName.Trim();
        WardType = wardType.Trim();
        TotalBeds = Math.Max(0, totalBeds);
        OccupiedBeds = Math.Clamp(occupiedBeds, 0, TotalBeds);
        AvailableBeds = Math.Max(0, TotalBeds - OccupiedBeds);
        PendingTransferCount = Math.Max(0, pendingTransfers);
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
    public Guid DepartmentId
    {
        get; private set;
    }
    public string DepartmentName { get; private set; } = string.Empty;
    public string WardType { get; private set; } = string.Empty;
    public int TotalBeds
    {
        get; private set;
    }
    public int OccupiedBeds
    {
        get; private set;
    }
    public int AvailableBeds
    {
        get; private set;
    }
    public int PendingTransferCount
    {
        get; private set;
    }
    public DateTime LastUpdatedUtc
    {
        get; private set;
    }

    public double OccupancyRatePercentage =>
        TotalBeds > 0 ? Math.Round((double)OccupiedBeds / TotalBeds * 100.0, 1) : 0.0;

    public void UpdateOccupancy(int totalBeds, int occupiedBeds, int pendingTransfers, DateTime updatedUtc)
    {
        TotalBeds = Math.Max(0, totalBeds);
        OccupiedBeds = Math.Clamp(occupiedBeds, 0, TotalBeds);
        AvailableBeds = Math.Max(0, TotalBeds - OccupiedBeds);
        PendingTransferCount = Math.Max(0, pendingTransfers);
        LastUpdatedUtc = updatedUtc;
    }
}
