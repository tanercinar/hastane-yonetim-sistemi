using HospitalManagement.Modules.Inpatient.Application;

namespace HospitalManagement.Modules.Inpatient.Infrastructure;

public sealed class NoOpInpatientRealtimeNotifier : IInpatientRealtimeNotifier
{
    public Task NotifyBedChangedAsync(Guid bedId, Guid wardId, string oldStatus, string newStatus, Guid? admissionId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyAdmissionChangedAsync(Guid admissionId, Guid wardId, string oldStatus, string newStatus, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyTransferChangedAsync(Guid transferId, Guid sourceWardId, Guid targetWardId, string status, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyDischargeCompletedAsync(Guid dischargeId, Guid admissionId, Guid wardId, string dischargeType, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
