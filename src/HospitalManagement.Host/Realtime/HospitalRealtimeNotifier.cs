using HospitalManagement.Contracts.Realtime;

using Microsoft.AspNetCore.SignalR;

namespace HospitalManagement.Host.Realtime;

public sealed class HospitalRealtimeNotifier : IHospitalRealtimeNotifier
{
    private readonly IHubContext<HospitalHub> _hubContext;

    public HospitalRealtimeNotifier(IHubContext<HospitalHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public Task NotifySlotChangedAsync(
        Guid slotId,
        Guid doctorId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var update = new SlotRealtimeUpdate(slotId, doctorId, status);
        return _hubContext.Clients.All.SendAsync("SlotStatusChanged", update, cancellationToken);
    }

    public async Task NotifyQueueUpdatedAsync(
        Guid appointmentId,
        Guid doctorId,
        Guid patientId,
        int? queueNumber,
        string status,
        CancellationToken cancellationToken = default)
    {
        var update = new QueueRealtimeUpdate(appointmentId, doctorId, queueNumber, status);
        await _hubContext.Clients.Group("staff-queue").SendAsync("QueueUpdated", update, cancellationToken);
        await _hubContext.Clients.Group($"doctor-{doctorId}").SendAsync("QueueUpdated", update, cancellationToken);
        await _hubContext.Clients.Group($"person-{patientId}").SendAsync("QueueUpdated", update, cancellationToken);
    }

    public Task NotifyNotificationReceivedAsync(
        Guid notificationId,
        Guid recipientPersonId,
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        var update = new NotificationRealtimeUpdate(notificationId, recipientPersonId, title, message);
        return _hubContext.Clients.Group($"person-{recipientPersonId}").SendAsync("NotificationReceived", update, cancellationToken);
    }
}
