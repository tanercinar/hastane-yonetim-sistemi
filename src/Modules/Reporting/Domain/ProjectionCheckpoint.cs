namespace HospitalManagement.Modules.Reporting.Domain;

public enum ProjectionStatus
{
    Active = 1,
    Rebuilding = 2,
    Error = 3,
}

public sealed class ProjectionCheckpoint
{
    private ProjectionCheckpoint()
    {
    }

    public ProjectionCheckpoint(string projectionName, int version = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);

        ProjectionName = projectionName.Trim();
        LastProcessedPosition = 0;
        LastProcessedTimestampUtc = DateTime.UtcNow;
        Status = ProjectionStatus.Active;
        Version = version;
    }

    public string ProjectionName { get; private set; } = string.Empty;
    public long LastProcessedPosition
    {
        get; private set;
    }
    public DateTime LastProcessedTimestampUtc
    {
        get; private set;
    }
    public ProjectionStatus Status
    {
        get; private set;
    }
    public int Version
    {
        get; private set;
    }
    public string? LastError
    {
        get; private set;
    }

    public void MarkRebuilding()
    {
        Status = ProjectionStatus.Rebuilding;
        LastProcessedPosition = 0;
        LastProcessedTimestampUtc = DateTime.UtcNow;
        LastError = null;
    }

    public void MarkActive(long position, DateTime timestampUtc)
    {
        Status = ProjectionStatus.Active;
        LastProcessedPosition = position;
        LastProcessedTimestampUtc = timestampUtc;
        LastError = null;
    }

    public void MarkError(string errorMessage)
    {
        Status = ProjectionStatus.Error;
        LastError = string.IsNullOrWhiteSpace(errorMessage) ? "Bilinmeyen hata" : errorMessage.Trim();
    }
}
