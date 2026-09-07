namespace HospitalManagement.Modules.Emergency.Application;

public interface IEmergencyRealtimeNotifier
{
    Task NotifyAdmissionCreatedAsync(Guid admissionId, string protocolNumber, string arrivalType, CancellationToken cancellationToken = default);
    Task NotifyTriageRecordedAsync(Guid admissionId, string protocolNumber, string triageLevel, CancellationToken cancellationToken = default);
    Task NotifyDoctorAssignedAsync(Guid admissionId, string protocolNumber, Guid doctorId, string? bedOrZone, CancellationToken cancellationToken = default);
    Task NotifyStatusChangedAsync(Guid admissionId, string protocolNumber, string oldStatus, string newStatus, CancellationToken cancellationToken = default);
    Task NotifyDashboardUpdatedAsync(CancellationToken cancellationToken = default);
}
