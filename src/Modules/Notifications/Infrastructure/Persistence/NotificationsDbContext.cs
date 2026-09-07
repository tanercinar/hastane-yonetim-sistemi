using HospitalManagement.Modules.Notifications.Domain;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options)
{
    public const string Schema = "notifications";
    public const string MigrationHistoryTable = "__EFMigrationsHistory_Notifications";

    public DbSet<NotificationOutboxEvent> OutboxEvents => Set<NotificationOutboxEvent>();

    public DbSet<InAppNotification> InAppNotifications => Set<InAppNotification>();

    public DbSet<MockDeliveryRecord> MockDeliveries => Set<MockDeliveryRecord>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
