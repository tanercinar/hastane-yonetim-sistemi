using System.Security.Claims;

namespace HospitalManagement.Modules.Notifications.Application;

public interface INotificationService
{
    Task<bool> EnqueueOutboxEventAsync(
        PublishOutboxEventCommand command,
        CancellationToken cancellationToken = default);

    Task<ProcessOutboxResultDto> ProcessOutboxAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InAppNotificationDto>> GetMyNotificationsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<bool> MarkAsReadAsync(
        ClaimsPrincipal user,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<NotificationPreferenceDto?> GetMyPreferenceAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<NotificationPreferenceDto?> UpdateMyPreferenceAsync(
        ClaimsPrincipal user,
        UpdateNotificationPreferenceCommand command,
        CancellationToken cancellationToken = default);
}
