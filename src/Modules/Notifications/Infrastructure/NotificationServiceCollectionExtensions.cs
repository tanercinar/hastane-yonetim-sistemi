using HospitalManagement.Modules.Notifications.Application;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.Modules.Notifications.Infrastructure;

public static class NotificationServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services.AddDbContext<NotificationsDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString(connectionStringName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    NotificationsDbContext.MigrationHistoryTable,
                    NotificationsDbContext.Schema));
        });

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationTransport, LocalNotificationCaptureTransport>();
        services.AddScoped<INotificationProviderPort, RetryingNotificationProviderPort>();
        services.AddScoped<ILocalNotificationCaptureQuery, LocalNotificationCaptureQuery>();

        return services;
    }
}
