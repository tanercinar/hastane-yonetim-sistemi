using HospitalManagement.Modules.Emergency.Application;

namespace HospitalManagement.Modules.Emergency.Infrastructure;

public sealed class NoOpEmergencyRealtimeNotifier : IEmergencyRealtimeNotifier
{
    public Task NotifyAdmissionCreatedAsync(Guid admissionId, string protocolNumber, string arrivalType, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyTriageRecordedAsync(Guid admissionId, string protocolNumber, string triageLevel, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyDoctorAssignedAsync(Guid admissionId, string protocolNumber, Guid doctorId, string? bedOrZone, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyStatusChangedAsync(Guid admissionId, string protocolNumber, string oldStatus, string newStatus, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyDashboardUpdatedAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
