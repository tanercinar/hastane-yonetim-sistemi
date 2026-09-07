namespace HospitalManagement.Modules.Reporting.Domain;

public sealed class DailyOutpatientMetric
{
    private DailyOutpatientMetric()
    {
    }

    public DailyOutpatientMetric(
        DateOnly date,
        Guid departmentId,
        string departmentName,
        Guid? doctorId,
        string? doctorName,
        DateTime createdUtc)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Bölüm kimliği boş olamaz.", nameof(departmentId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(departmentName);

        Id = Guid.NewGuid();
        Date = date;
        DepartmentId = departmentId;
        DepartmentName = departmentName.Trim();
        DoctorId = doctorId;
        DoctorName = string.IsNullOrWhiteSpace(doctorName) ? null : doctorName.Trim();
        TotalAppointments = 0;
        ScheduledCount = 0;
        CheckedInCount = 0;
        InProgressCount = 0;
        CompletedCount = 0;
        CancelledCount = 0;
        NoShowCount = 0;
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
    public Guid? DoctorId
    {
        get; private set;
    }
    public string? DoctorName
    {
        get; private set;
    }
    public int TotalAppointments
    {
        get; private set;
    }
    public int ScheduledCount
    {
        get; private set;
    }
    public int CheckedInCount
    {
        get; private set;
    }
    public int InProgressCount
    {
        get; private set;
    }
    public int CompletedCount
    {
        get; private set;
    }
    public int CancelledCount
    {
        get; private set;
    }
    public int NoShowCount
    {
        get; private set;
    }
    public DateTime LastUpdatedUtc
    {
        get; private set;
    }

    public void ApplyTransition(string previousStatus, string newStatus, DateTime updatedUtc)
    {
        DecrementStatus(previousStatus);
        IncrementStatus(newStatus);
        LastUpdatedUtc = updatedUtc;
    }

    private void IncrementStatus(string status)
    {
        switch (status?.Trim().ToLowerInvariant())
        {
            case "scheduled":
            case "booked":
                ScheduledCount++;
                TotalAppointments++;
                break;
            case "checkedin":
            case "checked_in":
            case "waiting":
                CheckedInCount++;
                break;
            case "inprogress":
            case "in_progress":
                InProgressCount++;
                break;
            case "completed":
            case "fulfilled":
                CompletedCount++;
                break;
            case "cancelled":
            case "canceled":
                CancelledCount++;
                break;
            case "noshow":
            case "no_show":
                NoShowCount++;
                break;
            default:
                ScheduledCount++;
                TotalAppointments++;
                break;
        }
    }

    private void DecrementStatus(string previousStatus)
    {
        if (string.IsNullOrWhiteSpace(previousStatus) || previousStatus.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        switch (previousStatus.Trim().ToLowerInvariant())
        {
            case "scheduled":
            case "booked":
                ScheduledCount = Math.Max(0, ScheduledCount - 1);
                break;
            case "checkedin":
            case "checked_in":
            case "waiting":
                CheckedInCount = Math.Max(0, CheckedInCount - 1);
                break;
            case "inprogress":
            case "in_progress":
                InProgressCount = Math.Max(0, InProgressCount - 1);
                break;
            case "completed":
            case "fulfilled":
                CompletedCount = Math.Max(0, CompletedCount - 1);
                break;
            case "cancelled":
            case "canceled":
                CancelledCount = Math.Max(0, CancelledCount - 1);
                break;
            case "noshow":
            case "no_show":
                NoShowCount = Math.Max(0, NoShowCount - 1);
                break;
        }
    }
}
