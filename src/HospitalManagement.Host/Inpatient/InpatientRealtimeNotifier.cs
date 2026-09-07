using HospitalManagement.Contracts.Inpatient;
using HospitalManagement.Host.Realtime;
using HospitalManagement.Modules.Inpatient.Application;
using Microsoft.AspNetCore.SignalR;

namespace HospitalManagement.Host.Inpatient;

public sealed class InpatientRealtimeNotifier : IInpatientRealtimeNotifier
{
    private readonly IHubContext<HospitalHub> _hubContext;

    public InpatientRealtimeNotifier(IHubContext<HospitalHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public async Task NotifyBedChangedAsync(
        Guid bedId,
        Guid wardId,
        string oldStatus,
        string newStatus,
        Guid? admissionId,
        CancellationToken cancellationToken = default)
    {
        var update = new
        {
            ChangedAtUtc = DateTime.UtcNow
        };

        await _hubContext.Clients.Group("inpatient-staff")
            .SendAsync("InpatientBedChanged", update, cancellationToken);
        await _hubContext.Clients.Group("inpatient-staff")
            .SendAsync("InpatientDashboardUpdated", update, cancellationToken);
    }

    public async Task NotifyAdmissionChangedAsync(
        Guid admissionId,
        Guid wardId,
        string oldStatus,
        string newStatus,
        CancellationToken cancellationToken = default)
    {
        var update = new
        {
            ChangedAtUtc = DateTime.UtcNow
        };

        await _hubContext.Clients.Group("inpatient-staff")
            .SendAsync("InpatientAdmissionChanged", update, cancellationToken);
        await _hubContext.Clients.Group("inpatient-staff")
            .SendAsync("InpatientDashboardUpdated", update, cancellationToken);
    }

    public async Task NotifyTransferChangedAsync(
        Guid transferId,
        Guid sourceWardId,
        Guid targetWardId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var update = new
        {
            ChangedAtUtc = DateTime.UtcNow
        };

        await _hubContext.Clients.Group("inpatient-staff")
            .SendAsync("InpatientTransferChanged", update, cancellationToken);
        await _hubContext.Clients.Group("inpatient-staff")
            .SendAsync("InpatientDashboardUpdated", update, cancellationToken);
    }

    public async Task NotifyDischargeCompletedAsync(
        Guid dischargeId,
        Guid admissionId,
        Guid wardId,
        string dischargeType,
        CancellationToken cancellationToken = default)
    {
        var update = new
        {
            ChangedAtUtc = DateTime.UtcNow
        };

        await _hubContext.Clients.Group("inpatient-staff")
            .SendAsync("InpatientDischargeCompleted", update, cancellationToken);
        await _hubContext.Clients.Group("inpatient-staff")
            .SendAsync("InpatientDashboardUpdated", update, cancellationToken);
    }
}
