namespace HospitalManagement.Contracts.Realtime;

public sealed record SlotRealtimeUpdate(
    Guid SlotId,
    Guid DoctorId,
    string Status);

public sealed record QueueRealtimeUpdate(
    Guid AppointmentId,
    Guid DoctorId,
    int? QueueNumber,
    string Status);

public sealed record NotificationRealtimeUpdate(
    Guid NotificationId,
    Guid RecipientPersonId,
    string Title,
    string Message);

public sealed record CriticalResultRealtimeUpdate(
    Guid NotificationId,
    string Status);
