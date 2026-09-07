using HospitalManagement.Modules.Notifications.Application;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Notifications.Infrastructure;

public sealed class LocalNotificationCaptureQuery(NotificationsDbContext dbContext)
    : ILocalNotificationCaptureQuery
{
    private readonly NotificationsDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<IReadOnlyList<LocalNotificationCaptureDto>> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        return await _dbContext.MockDeliveries
            .AsNoTracking()
            .Where(delivery => delivery.IdempotencyKey == idempotencyKey)
            .OrderBy(delivery => delivery.Channel)
            .Select(delivery => new LocalNotificationCaptureDto(
                delivery.Id,
                delivery.Channel,
                delivery.Recipient,
                delivery.Subject,
                delivery.Body,
                delivery.IdempotencyKey,
                delivery.TemplateKey,
                delivery.Locale,
                delivery.Provider,
                delivery.AttemptCount,
                delivery.SentAtUtc))
            .ToListAsync(cancellationToken);
    }
}
