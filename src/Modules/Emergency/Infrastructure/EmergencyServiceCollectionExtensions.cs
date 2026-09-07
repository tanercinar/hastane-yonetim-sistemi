using HospitalManagement.Modules.Emergency.Application;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HospitalManagement.Modules.Emergency.Infrastructure;

public static class EmergencyServiceCollectionExtensions
{
    public const string SchemaName = "emergency";

    public static IServiceCollection AddEmergencyModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services.AddDbContext<EmergencyDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString(connectionStringName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory_Emergency",
                    SchemaName));
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.TryAddScoped<IEmergencyRealtimeNotifier, NoOpEmergencyRealtimeNotifier>();
        services.AddScoped<IEmergencyDataSeeder, EmergencyDataSeeder>();
        services.AddScoped<IEmergencyAdmissionService, EmergencyAdmissionService>();
        services.AddScoped<IEmergencyTrackingBoardService, EmergencyTrackingBoardService>();
        services.AddScoped<IEmergencyEncounterService, EmergencyEncounterService>();

        return services;
    }
}
