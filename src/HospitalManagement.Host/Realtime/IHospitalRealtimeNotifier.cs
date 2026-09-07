using HospitalManagement.Contracts.Realtime;

namespace HospitalManagement.Host.Realtime;

public interface IHospitalRealtimeNotifier
{
    Task NotifySlotChangedAsync(
        Guid slotId,
        Guid doctorId,
        string status,
        CancellationToken cancellationToken = default);

    Task NotifyQueueUpdatedAsync(
        Guid appointmentId,
        Guid doctorId,
        Guid patientId,
        int? queueNumber,
        string status,
        CancellationToken cancellationToken = default);

    Task NotifyNotificationReceivedAsync(
        Guid notificationId,
        Guid recipientPersonId,
        string title,
        string message,
        CancellationToken cancellationToken = default);
}
