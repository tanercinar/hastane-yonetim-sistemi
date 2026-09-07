namespace HospitalManagement.BuildingBlocks.Audit;

public sealed record AuditLogFilter(
    Guid? ActorUserId = null,
    Guid? ActorPersonId = null,
    string? Action = null,
    string? TargetResourceType = null,
    string? TargetResourceId = null,
    AuditOutcome? Outcome = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 50);
