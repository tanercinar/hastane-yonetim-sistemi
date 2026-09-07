using HospitalManagement.Modules.Scheduling.Application;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.Modules.Scheduling.Infrastructure;

public static class SchedulingServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulingModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services.AddDbContext<SchedulingDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString(connectionStringName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    SchedulingDbContext.MigrationHistoryTable,
                    SchedulingDbContext.Schema));
        });

        services.AddScoped<ISchedulingService, SchedulingService>();
        services.AddScoped<ISchedulingDataSeeder, SchedulingDataSeeder>();

        return services;
    }
}
