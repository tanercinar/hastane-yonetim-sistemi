namespace HospitalManagement.BuildingBlocks.Audit;

public interface IAuditEventPublisher
{
    Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
