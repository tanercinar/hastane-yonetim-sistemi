using HospitalManagement.Modules.Interoperability.Domain.Hl7;

namespace HospitalManagement.Modules.Interoperability.Application;

public sealed record Hl7DeadLetterEntryDto(
    Guid Id,
    string MessageControlId,
    string MessageType,
    string FailureReason,
    string PayloadSummary,
    DateTime ReceivedAtUtc,
    int RetryCount,
    bool IsResolved,
    DateTime? ResolvedAtUtc);

public interface IHl7V2Service
{
    Task<Hl7V2Ack> ProcessInboundMessageAsync(string rawEr7Message, CancellationToken cancellationToken = default);
    Task<string> GenerateAdtA01MessageAsync(Guid patientId, string protocolNumber, string wardName, string bedNumber, CancellationToken cancellationToken = default);
    Task<string> GenerateAdtA03MessageAsync(Guid patientId, string protocolNumber, DateTime dischargeDateUtc, CancellationToken cancellationToken = default);
    Task<string> GenerateOrmO01MessageAsync(Guid orderId, Guid patientId, string testCode, string testName, CancellationToken cancellationToken = default);
    Task<string> GenerateOruR01MessageAsync(Guid orderId, Guid patientId, string testCode, string resultValue, string units, CancellationToken cancellationToken = default);
    Task<List<Hl7DeadLetterEntryDto>> GetDeadLetterEntriesAsync(CancellationToken cancellationToken = default);
    Task<bool> RetryDeadLetterEntryAsync(Guid id, CancellationToken cancellationToken = default);
}
