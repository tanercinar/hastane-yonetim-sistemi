using HospitalManagement.Modules.Notifications.Application;
using HospitalManagement.Modules.Notifications.Domain;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Notifications.Infrastructure;

public sealed class LocalNotificationCaptureTransport(
    NotificationsDbContext dbContext,
    TimeProvider timeProvider) : INotificationTransport
{
    private readonly NotificationsDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly TimeProvider _timeProvider = timeProvider
        ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task CaptureAsync(
        NotificationProviderMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var exists = await _dbContext.MockDeliveries.AnyAsync(
            delivery => delivery.IdempotencyKey == message.IdempotencyKey
                && delivery.Channel == message.Channel,
            cancellationToken);
        if (exists)
        {
            return;
        }

        _dbContext.MockDeliveries.Add(MockDeliveryRecord.Create(
            Guid.NewGuid(),
            message.Channel,
            message.Recipient,
            message.Subject,
            message.Body,
            message.IdempotencyKey,
            _timeProvider.GetUtcNow().UtcDateTime,
            message.TemplateKey,
            message.Locale,
            "MOCK-LOCAL-CAPTURE",
            message.AttemptCount));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
