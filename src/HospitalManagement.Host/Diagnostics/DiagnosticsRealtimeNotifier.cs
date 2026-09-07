using HospitalManagement.Contracts.Realtime;
using HospitalManagement.Host.Realtime;
using HospitalManagement.Modules.Diagnostics.Application;

using Microsoft.AspNetCore.SignalR;

namespace HospitalManagement.Host.Diagnostics;

public sealed class DiagnosticsRealtimeNotifier(
    IHubContext<HospitalHub> hubContext) : IDiagnosticsRealtimeNotifier
{
    private readonly IHubContext<HospitalHub> _hubContext = hubContext;

    public Task NotifyCriticalResultChangedAsync(
        Guid notificationId,
        Guid recipientPersonId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var update = new CriticalResultRealtimeUpdate(notificationId, status);
        return _hubContext.Clients
            .Group($"person-{recipientPersonId}")
            .SendAsync("CriticalResultChanged", update, cancellationToken);
    }
}
