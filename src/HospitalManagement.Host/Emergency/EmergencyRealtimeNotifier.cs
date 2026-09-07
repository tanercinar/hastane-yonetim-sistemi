using HospitalManagement.Host.Realtime;
using HospitalManagement.Modules.Emergency.Application;
using Microsoft.AspNetCore.SignalR;

namespace HospitalManagement.Host.Emergency;

public sealed class EmergencyRealtimeNotifier : IEmergencyRealtimeNotifier
{
    public const string EmergencyStaffGroup = "emergency-staff";

    private readonly IHubContext<HospitalHub> _hubContext;

    public EmergencyRealtimeNotifier(IHubContext<HospitalHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public async Task NotifyAdmissionCreatedAsync(
        Guid admissionId,
        string protocolNumber,
        string arrivalType,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(EmergencyStaffGroup).SendAsync(
            "EmergencyAdmissionCreated",
            new
            {
                ChangedAtUtc = DateTime.UtcNow,
            },
            cancellationToken);

        await NotifyDashboardUpdatedAsync(cancellationToken);
    }

    public async Task NotifyTriageRecordedAsync(
        Guid admissionId,
        string protocolNumber,
        string triageLevel,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(EmergencyStaffGroup).SendAsync(
            "EmergencyTriageRecorded",
            new
            {
                ChangedAtUtc = DateTime.UtcNow,
            },
            cancellationToken);

        await NotifyDashboardUpdatedAsync(cancellationToken);
    }

    public async Task NotifyDoctorAssignedAsync(
        Guid admissionId,
        string protocolNumber,
        Guid doctorId,
        string? bedOrZone,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(EmergencyStaffGroup).SendAsync(
            "EmergencyDoctorAssigned",
            new
            {
                ChangedAtUtc = DateTime.UtcNow,
            },
            cancellationToken);

        await NotifyDashboardUpdatedAsync(cancellationToken);
    }

    public async Task NotifyStatusChangedAsync(
        Guid admissionId,
        string protocolNumber,
        string oldStatus,
        string newStatus,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(EmergencyStaffGroup).SendAsync(
            "EmergencyStatusChanged",
            new
            {
                ChangedAtUtc = DateTime.UtcNow,
            },
            cancellationToken);

        await NotifyDashboardUpdatedAsync(cancellationToken);
    }

    public async Task NotifyDashboardUpdatedAsync(CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(EmergencyStaffGroup).SendAsync(
            "EmergencyDashboardUpdated",
            new
            {
                ChangedAtUtc = DateTime.UtcNow,
            },
            cancellationToken);
    }
}
