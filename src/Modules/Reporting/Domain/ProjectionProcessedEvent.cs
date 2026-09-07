namespace HospitalManagement.Modules.Reporting.Domain;

public sealed class ProjectionProcessedEvent
{
    private ProjectionProcessedEvent()
    {
    }

    public ProjectionProcessedEvent(Guid eventId, string projectionName, DateTime processedAtUtc)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("EventId boş olamaz.", nameof(eventId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);

        EventId = eventId;
        ProjectionName = projectionName.Trim();
        ProcessedAtUtc = processedAtUtc;
    }

    public Guid EventId
    {
        get; private set;
    }
    public string ProjectionName { get; private set; } = string.Empty;
    public DateTime ProcessedAtUtc
    {
        get; private set;
    }
}
