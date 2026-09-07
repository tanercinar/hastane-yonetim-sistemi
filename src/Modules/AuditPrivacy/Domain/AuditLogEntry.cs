using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using HospitalManagement.BuildingBlocks.Audit;

namespace HospitalManagement.Modules.AuditPrivacy.Domain;

public sealed class AuditLogEntry
{
    private AuditLogEntry()
    {
    }

    public Guid Id
    {
        get; private set;
    }

    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public Guid? ActorUserId
    {
        get; private set;
    }

    public Guid? ActorPersonId
    {
        get; private set;
    }

    public string? ActorRole
    {
        get; private set;
    }

    public string? ActorIpAddress
    {
        get; private set;
    }

    public string? ActorUserAgent
    {
        get; private set;
    }

    public string Action { get; private set; } = string.Empty;

    public string TargetResourceType { get; private set; } = string.Empty;

    public string TargetResourceId { get; private set; } = string.Empty;

    public string Outcome { get; private set; } = string.Empty;

    public string? Reason
    {
        get; private set;
    }

    public string CorrelationId { get; private set; } = string.Empty;

    public string? DetailsJson
    {
        get; private set;
    }

    public string RecordHash { get; private set; } = string.Empty;

    public string? PreviousRecordHash
    {
        get; private set;
    }

    public long ChainPosition
    {
        get; private set;
    }

    public int HashVersion
    {
        get; private set;
    }

    public static AuditLogEntry Create(
        Guid id,
        DateTime createdAtUtc,
        Guid? actorUserId,
        Guid? actorPersonId,
        string? actorRole,
        string? actorIpAddress,
        string? actorUserAgent,
        string action,
        string targetResourceType,
        string targetResourceId,
        AuditOutcome outcome,
        string? reason,
        string correlationId,
        string? detailsJson = null,
        string? previousRecordHash = null,
        long chainPosition = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetResourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetResourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        var normalizedCreatedAt = new DateTime(
            createdAtUtc.Ticks - (createdAtUtc.Ticks % TimeSpan.TicksPerMillisecond),
            DateTimeKind.Utc);

        var outcomeString = outcome.ToString();
        var recordHash = ComputeRecordHash(
            id,
            normalizedCreatedAt,
            actorUserId,
            actorPersonId,
            action,
            targetResourceType,
            targetResourceId,
            outcomeString,
            correlationId,
            previousRecordHash,
            actorRole,
            actorIpAddress,
            actorUserAgent,
            reason,
            detailsJson,
            chainPosition);

        return new AuditLogEntry
        {
            Id = id,
            CreatedAtUtc = normalizedCreatedAt,
            ActorUserId = actorUserId,
            ActorPersonId = actorPersonId,
            ActorRole = actorRole,
            ActorIpAddress = actorIpAddress,
            ActorUserAgent = actorUserAgent,
            Action = action,
            TargetResourceType = targetResourceType,
            TargetResourceId = targetResourceId,
            Outcome = outcomeString,
            Reason = reason,
            CorrelationId = correlationId,
            DetailsJson = detailsJson,
            RecordHash = recordHash,
            PreviousRecordHash = previousRecordHash,
            ChainPosition = chainPosition,
            HashVersion = 2,
        };
    }

    public static AuditLogEntry FromEvent(
        AuditEvent auditEvent,
        string? previousRecordHash = null,
        long chainPosition = 0)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        return Create(
            auditEvent.Id == Guid.Empty ? Guid.NewGuid() : auditEvent.Id,
            auditEvent.CreatedAtUtc == default ? DateTime.UtcNow : auditEvent.CreatedAtUtc,
            auditEvent.ActorUserId,
            auditEvent.ActorPersonId,
            auditEvent.ActorRole,
            auditEvent.ActorIpAddress,
            auditEvent.ActorUserAgent,
            auditEvent.Action,
            auditEvent.TargetResourceType,
            auditEvent.TargetResourceId,
            auditEvent.Outcome,
            auditEvent.Reason,
            auditEvent.CorrelationId,
            auditEvent.DetailsJson,
            previousRecordHash,
            chainPosition);
    }

    public bool VerifyHashIntegrity()
    {
        var computed = HashVersion <= 1
            ? ComputeLegacyRecordHash(
                Id,
                CreatedAtUtc,
                ActorUserId,
                ActorPersonId,
                Action,
                TargetResourceType,
                TargetResourceId,
                Outcome,
                CorrelationId,
                PreviousRecordHash)
            : ComputeRecordHash(
                Id,
                CreatedAtUtc,
                ActorUserId,
                ActorPersonId,
                Action,
                TargetResourceType,
                TargetResourceId,
                Outcome,
                CorrelationId,
                PreviousRecordHash,
                ActorRole,
                ActorIpAddress,
                ActorUserAgent,
                Reason,
                DetailsJson,
                ChainPosition);

        return string.Equals(RecordHash, computed, StringComparison.Ordinal);
    }

    public static string ComputeRecordHash(
        Guid id,
        DateTime createdAtUtc,
        Guid? actorUserId,
        Guid? actorPersonId,
        string action,
        string targetResourceType,
        string targetResourceId,
        string outcome,
        string correlationId,
        string? previousRecordHash = null,
        string? actorRole = null,
        string? actorIpAddress = null,
        string? actorUserAgent = null,
        string? reason = null,
        string? detailsJson = null,
        long chainPosition = 0)
    {
        var timestampIso = createdAtUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture);
        var actorUser = actorUserId.HasValue ? actorUserId.Value.ToString("D") : string.Empty;
        var actorPerson = actorPersonId.HasValue ? actorPersonId.Value.ToString("D") : string.Empty;

        var raw = JsonSerializer.Serialize(new
        {
            Id = id.ToString("D"),
            CreatedAtUtc = timestampIso,
            ActorUserId = actorUser,
            ActorPersonId = actorPerson,
            ActorRole = actorRole ?? string.Empty,
            ActorIpAddress = actorIpAddress ?? string.Empty,
            ActorUserAgent = actorUserAgent ?? string.Empty,
            Action = action,
            TargetResourceType = targetResourceType,
            TargetResourceId = targetResourceId,
            Outcome = outcome,
            Reason = reason ?? string.Empty,
            CorrelationId = correlationId,
            DetailsJson = detailsJson ?? string.Empty,
            PreviousRecordHash = previousRecordHash ?? string.Empty,
            ChainPosition = chainPosition,
        });
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hashBytes);
    }

    private static string ComputeLegacyRecordHash(
        Guid id,
        DateTime createdAtUtc,
        Guid? actorUserId,
        Guid? actorPersonId,
        string action,
        string targetResourceType,
        string targetResourceId,
        string outcome,
        string correlationId,
        string? previousRecordHash)
    {
        var timestampIso = createdAtUtc.ToUniversalTime().ToString(
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            System.Globalization.CultureInfo.InvariantCulture);
        var actorUser = actorUserId?.ToString("D") ?? string.Empty;
        var actorPerson = actorPersonId?.ToString("D") ?? string.Empty;
        var raw = $"{id:D}|{timestampIso}|{actorUser}|{actorPerson}|{action}|{targetResourceType}|{targetResourceId}|{outcome}|{correlationId}|{previousRecordHash}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}
