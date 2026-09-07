using HospitalManagement.Contracts.Notifications;

namespace HospitalManagement.Web.Client.Notifications;

public interface INotificationApiClient
{
    Task<IReadOnlyList<NotificationDetailResponse>?> GetMyNotificationsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);
}
