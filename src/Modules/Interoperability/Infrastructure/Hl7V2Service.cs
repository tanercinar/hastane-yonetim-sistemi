using HospitalManagement.Modules.Interoperability.Application;
using HospitalManagement.Modules.Interoperability.Domain;
using HospitalManagement.Modules.Interoperability.Domain.Hl7;
using HospitalManagement.Modules.Interoperability.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Interoperability.Infrastructure;

public sealed class Hl7V2Service : IHl7V2Service
{
    private readonly InteroperabilityDbContext _dbContext;
    private readonly IIntegrationMockEngine _mockEngine;

    public Hl7V2Service(
        InteroperabilityDbContext dbContext,
        IIntegrationMockEngine mockEngine)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mockEngine = mockEngine ?? throw new ArgumentNullException(nameof(mockEngine));
    }

    public async Task<Hl7V2Ack> ProcessInboundMessageAsync(string rawEr7Message, CancellationToken cancellationToken = default)
    {
        var parseResult = Hl7Er7Engine.ParseMessage(rawEr7Message);
        if (!parseResult.IsValid || parseResult.Message is null)
        {
            var failureReason = parseResult.ErrorReason ?? "Geçersiz HL7 v2 ER7 mesajı";
            var deadLetter = new Hl7DeadLetterEntry(
                "UNKNOWN",
                "UNKNOWN",
                failureReason,
                MaskPayload(rawEr7Message));

            _dbContext.Hl7DeadLetterEntries.Add(deadLetter);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Hl7Er7Engine.GenerateAck("UNKNOWN", "AE", failureReason);
        }

        var msg = parseResult.Message;

        var execResult = await _mockEngine.ExecuteAsync(
            ExternalSystemType.Hl7V2,
            $"Inbound_{msg.MessageType}^{msg.TriggerEvent}",
            () =>
            {
                // Inbound processing simulation (e.g. mapping ADT/ORM/ORU to internal clinical aggregate)
                return Task.FromResult(Hl7Er7Engine.GenerateAck(msg.MessageControlId, "AA", "Mesaj başarıyla işlendi."));
            },
            payloadSummary: $"{{\"msgType\": \"{msg.MessageType}^{msg.TriggerEvent}\", \"controlId\": \"{msg.MessageControlId}\"}}",
            correlationId: $"CORR-HL7-{msg.MessageControlId}",
            cancellationToken: cancellationToken);

        if (!execResult.IsSuccess)
        {
            var deadLetter = new Hl7DeadLetterEntry(
                msg.MessageControlId,
                $"{msg.MessageType}^{msg.TriggerEvent}",
                execResult.ErrorMessage ?? "İşlem sırasında hata oluştu.",
                MaskPayload(rawEr7Message));

            _dbContext.Hl7DeadLetterEntries.Add(deadLetter);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Hl7Er7Engine.GenerateAck(msg.MessageControlId, "AE", execResult.ErrorMessage ?? "HL7 v2 işleme hatası.");
        }

        return execResult.Value ?? Hl7Er7Engine.GenerateAck(msg.MessageControlId, "AA", "Mesaj kabul edildi.");
    }

    public Task<string> GenerateAdtA01MessageAsync(Guid patientId, string protocolNumber, string wardName, string bedNumber, CancellationToken cancellationToken = default)
    {
        var er7 = Hl7Er7Engine.GenerateAdtA01(patientId, protocolNumber, wardName, bedNumber);
        return Task.FromResult(er7);
    }

    public Task<string> GenerateAdtA03MessageAsync(Guid patientId, string protocolNumber, DateTime dischargeDateUtc, CancellationToken cancellationToken = default)
    {
        var er7 = Hl7Er7Engine.GenerateAdtA03(patientId, protocolNumber, dischargeDateUtc);
        return Task.FromResult(er7);
    }

    public Task<string> GenerateOrmO01MessageAsync(Guid orderId, Guid patientId, string testCode, string testName, CancellationToken cancellationToken = default)
    {
        var er7 = Hl7Er7Engine.GenerateOrmO01(orderId, patientId, testCode, testName);
        return Task.FromResult(er7);
    }

    public Task<string> GenerateOruR01MessageAsync(Guid orderId, Guid patientId, string testCode, string resultValue, string units, CancellationToken cancellationToken = default)
    {
        var er7 = Hl7Er7Engine.GenerateOruR01(orderId, patientId, testCode, resultValue, units);
        return Task.FromResult(er7);
    }

    public async Task<List<Hl7DeadLetterEntryDto>> GetDeadLetterEntriesAsync(CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.Hl7DeadLetterEntries
            .AsNoTracking()
            .OrderByDescending(e => e.ReceivedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        return list.Select(e => new Hl7DeadLetterEntryDto(
            e.Id,
            e.MessageControlId,
            e.MessageType,
            e.FailureReason,
            e.PayloadSummary,
            e.ReceivedAtUtc,
            e.RetryCount,
            e.IsResolved,
            e.ResolvedAtUtc)).ToList();
    }

    public async Task<bool> RetryDeadLetterEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _dbContext.Hl7DeadLetterEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        // Simulate retry processing
        entry.RecordRetry(true);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static readonly string[] LineDelimiters = ["\r\n", "\r", "\n"];

    private static string MaskPayload(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "{}";
        }

        // Return segment summaries without raw patient identifying text
        var lines = raw.Split(LineDelimiters, StringSplitOptions.RemoveEmptyEntries);
        var segmentTypes = lines.Select(l => l.Length >= 3 ? l[..3] : "???").ToList();
        return $"{{\"segmentCount\": {lines.Length}, \"segments\": [\"{string.Join("\", \"", segmentTypes)}\"]}}";
    }
}
