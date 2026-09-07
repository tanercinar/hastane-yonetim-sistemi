namespace HospitalManagement.BuildingBlocks.Audit;

public sealed record AuditEvent(
    Guid Id,
    DateTime CreatedAtUtc,
    Guid? ActorUserId,
    Guid? ActorPersonId,
    string? ActorRole,
    string? ActorIpAddress,
    string? ActorUserAgent,
    string Action,
    string TargetResourceType,
    string TargetResourceId,
    AuditOutcome Outcome,
    string? Reason,
    string CorrelationId,
    string? DetailsJson = null);
