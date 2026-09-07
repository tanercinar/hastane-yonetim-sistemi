namespace HospitalManagement.BuildingBlocks.Audit;

public interface IAuditLogReader
{
    Task<IReadOnlyList<AuditEvent>> QueryLogsAsync(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyTamperIntegrityAsync(
        Guid logId,
        CancellationToken cancellationToken = default);
}
