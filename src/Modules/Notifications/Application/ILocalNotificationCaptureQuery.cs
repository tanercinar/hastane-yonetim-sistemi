namespace HospitalManagement.Modules.Notifications.Application;

public interface ILocalNotificationCaptureQuery
{
    Task<IReadOnlyList<LocalNotificationCaptureDto>> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
