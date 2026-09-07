namespace HospitalManagement.Modules.Inpatient.Application;

public interface IInpatientRealtimeNotifier
{
    Task NotifyBedChangedAsync(
        Guid bedId,
        Guid wardId,
        string oldStatus,
        string newStatus,
        Guid? admissionId,
        CancellationToken cancellationToken = default);

    Task NotifyAdmissionChangedAsync(
        Guid admissionId,
        Guid wardId,
        string oldStatus,
        string newStatus,
        CancellationToken cancellationToken = default);

    Task NotifyTransferChangedAsync(
        Guid transferId,
        Guid sourceWardId,
        Guid targetWardId,
        string status,
        CancellationToken cancellationToken = default);

    Task NotifyDischargeCompletedAsync(
        Guid dischargeId,
        Guid admissionId,
        Guid wardId,
        string dischargeType,
        CancellationToken cancellationToken = default);
}
