namespace HospitalManagement.Modules.Diagnostics.Application;

public interface IDiagnosticsRealtimeNotifier
{
    Task NotifyCriticalResultChangedAsync(
        Guid notificationId,
        Guid recipientPersonId,
        string status,
        CancellationToken cancellationToken = default);
}
