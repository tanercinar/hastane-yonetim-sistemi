namespace HospitalManagement.Modules.Interoperability.Domain;

public sealed class IntegrationMessageLog
{
    private IntegrationMessageLog()
    {
    }

    public IntegrationMessageLog(
        string correlationId,
        ExternalSystemType systemType,
        IntegrationMessageDirection direction,
        string actionName,
        string payloadSummary,
        IntegrationMessageStatus status,
        int retryCount,
        int durationMs,
        string? errorMessage = null)
    {
        Id = Guid.NewGuid();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? $"CORR-{Guid.NewGuid():N}" : correlationId;
        SystemType = systemType;
        Direction = direction;
        ActionName = string.IsNullOrWhiteSpace(actionName) ? "DefaultAction" : actionName.Trim();
        PayloadSummary = string.IsNullOrWhiteSpace(payloadSummary) ? "{}" : payloadSummary.Trim();
        Status = status;
        RetryCount = Math.Max(0, retryCount);
        DurationMs = Math.Max(0, durationMs);
        ErrorMessage = errorMessage?.Trim();
        TimestampUtc = DateTime.UtcNow;
    }

    public Guid Id
    {
        get; private set;
    }
    public string CorrelationId { get; private set; } = string.Empty;
    public ExternalSystemType SystemType
    {
        get; private set;
    }
    public IntegrationMessageDirection Direction
    {
        get; private set;
    }
    public string ActionName { get; private set; } = string.Empty;
    public string PayloadSummary { get; private set; } = string.Empty;
    public IntegrationMessageStatus Status
    {
        get; private set;
    }
    public int RetryCount
    {
        get; private set;
    }
    public int DurationMs
    {
        get; private set;
    }
    public string? ErrorMessage
    {
        get; private set;
    }
    public DateTime TimestampUtc
    {
        get; private set;
    }
}
