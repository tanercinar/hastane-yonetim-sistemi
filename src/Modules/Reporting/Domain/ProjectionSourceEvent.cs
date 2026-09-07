namespace HospitalManagement.Modules.Reporting.Domain;

/// <summary>
/// Durable, minimized input journal used to rebuild reporting read models.
/// Payloads contain operational projection fields only and never patient clinical content.
/// </summary>
public sealed class ProjectionSourceEvent
{
    private ProjectionSourceEvent()
    {
    }

    public ProjectionSourceEvent(
        Guid eventId,
        string projectionName,
        string eventType,
        string payloadJson,
        DateTime occurredAtUtc)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("EventId boş olamaz.", nameof(eventId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        EventId = eventId;
        ProjectionName = projectionName.Trim();
        EventType = eventType.Trim();
        PayloadJson = payloadJson;
        OccurredAtUtc = occurredAtUtc;
    }

    public long Position
    {
        get; private set;
    }
    public Guid EventId
    {
        get; private set;
    }
    public string ProjectionName { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc
    {
        get; private set;
    }
}
