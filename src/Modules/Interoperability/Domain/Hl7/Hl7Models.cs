namespace HospitalManagement.Modules.Interoperability.Domain.Hl7;

public sealed record Hl7V2Message(
    string MessageControlId,
    string MessageType,
    string TriggerEvent,
    DateTime TimestampUtc,
    string RawEr7Content);

public sealed record Hl7V2Ack(
    string MessageControlId,
    string AckCode, // AA (Application Accept), AE (Application Error), AR (Application Reject)
    string TextMessage,
    string RawEr7Content);

public sealed class Hl7DeadLetterEntry
{
    private Hl7DeadLetterEntry()
    {
    }

    public Hl7DeadLetterEntry(
        string messageControlId,
        string messageType,
        string failureReason,
        string payloadSummary)
    {
        Id = Guid.NewGuid();
        MessageControlId = string.IsNullOrWhiteSpace(messageControlId) ? $"MSG-{Guid.NewGuid():N}" : messageControlId.Trim();
        MessageType = string.IsNullOrWhiteSpace(messageType) ? "UNKNOWN" : messageType.Trim();
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? "Bilinmeyen hata" : failureReason.Trim();
        PayloadSummary = string.IsNullOrWhiteSpace(payloadSummary) ? "{}" : payloadSummary.Trim();
        ReceivedAtUtc = DateTime.UtcNow;
        RetryCount = 0;
        IsResolved = false;
    }

    public Guid Id
    {
        get; private set;
    }
    public string MessageControlId { get; private set; } = string.Empty;
    public string MessageType { get; private set; } = string.Empty;
    public string FailureReason { get; private set; } = string.Empty;
    public string PayloadSummary { get; private set; } = string.Empty;
    public DateTime ReceivedAtUtc
    {
        get; private set;
    }
    public int RetryCount
    {
        get; private set;
    }
    public bool IsResolved
    {
        get; private set;
    }
    public DateTime? ResolvedAtUtc
    {
        get; private set;
    }

    public void RecordRetry(bool success, string? newFailureReason = null)
    {
        RetryCount++;
        if (success)
        {
            IsResolved = true;
            ResolvedAtUtc = DateTime.UtcNow;
        }
        else if (!string.IsNullOrWhiteSpace(newFailureReason))
        {
            FailureReason = newFailureReason.Trim();
        }
    }
}
