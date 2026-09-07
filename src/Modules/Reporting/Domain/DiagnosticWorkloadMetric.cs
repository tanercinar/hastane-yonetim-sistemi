namespace HospitalManagement.Modules.Reporting.Domain;

public sealed class DiagnosticWorkloadMetric
{
    private DiagnosticWorkloadMetric()
    {
    }

    public DiagnosticWorkloadMetric(
        DateOnly date,
        string modalityOrSection,
        DateTime createdUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modalityOrSection);

        Id = Guid.NewGuid();
        Date = date;
        ModalityOrSection = modalityOrSection.Trim();
        TotalOrders = 0;
        PendingSpecimenCount = 0;
        ProcessingCount = 0;
        FinalizedCount = 0;
        CriticalCount = 0;
        AvgTurnaroundMinutes = 0.0;
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
    public string ModalityOrSection { get; private set; } = string.Empty;
    public int TotalOrders
    {
        get; private set;
    }
    public int PendingSpecimenCount
    {
        get; private set;
    }
    public int ProcessingCount
    {
        get; private set;
    }
    public int FinalizedCount
    {
        get; private set;
    }
    public int CriticalCount
    {
        get; private set;
    }
    public double AvgTurnaroundMinutes
    {
        get; private set;
    }
    public DateTime LastUpdatedUtc
    {
        get; private set;
    }

    public void ApplyOrderTransition(
        string previousStatus,
        string newStatus,
        bool isCritical,
        double? turnaroundMinutes,
        DateTime updatedUtc)
    {
        DecrementStatus(previousStatus);
        IncrementStatus(newStatus);

        if (isCritical)
        {
            CriticalCount++;
        }

        if (turnaroundMinutes.HasValue && turnaroundMinutes.Value > 0)
        {
            if (FinalizedCount <= 1)
            {
                AvgTurnaroundMinutes = turnaroundMinutes.Value;
            }
            else
            {
                AvgTurnaroundMinutes = Math.Round(((AvgTurnaroundMinutes * (FinalizedCount - 1)) + turnaroundMinutes.Value) / FinalizedCount, 1);
            }
        }

        LastUpdatedUtc = updatedUtc;
    }

    private void IncrementStatus(string status)
    {
        switch (status?.Trim().ToLowerInvariant())
        {
            case "ordered":
            case "requested":
                TotalOrders++;
                PendingSpecimenCount++;
                break;
            case "collected":
            case "received":
            case "inprogress":
            case "in_progress":
                ProcessingCount++;
                break;
            case "finalized":
            case "completed":
            case "signed":
                FinalizedCount++;
                break;
            default:
                TotalOrders++;
                PendingSpecimenCount++;
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
            case "ordered":
            case "requested":
                PendingSpecimenCount = Math.Max(0, PendingSpecimenCount - 1);
                break;
            case "collected":
            case "received":
            case "inprogress":
            case "in_progress":
                ProcessingCount = Math.Max(0, ProcessingCount - 1);
                break;
            case "finalized":
            case "completed":
            case "signed":
                FinalizedCount = Math.Max(0, FinalizedCount - 1);
                break;
        }
    }
}
