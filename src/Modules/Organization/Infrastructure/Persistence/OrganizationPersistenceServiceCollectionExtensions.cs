using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.Modules.Organization.Infrastructure.Persistence;

public static class OrganizationPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddOrganizationPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionString);

        services.AddDbContext<OrganizationDbContext>(options =>
            options
                .UseNpgsql(connectionString, provider => provider.MigrationsHistoryTable(
                    OrganizationDbContext.MigrationHistoryTable,
                    OrganizationDbContext.Schema))
                .EnableDetailedErrors(false)
                .EnableSensitiveDataLogging(false));

        services.AddScoped<IOrganizationDataSeeder, OrganizationDataSeeder>();

        return services;
    }

    public static IServiceCollection AddOrganizationPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services.AddDbContext<OrganizationDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString(connectionStringName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            options
                .UseNpgsql(connectionString, provider => provider.MigrationsHistoryTable(
                    OrganizationDbContext.MigrationHistoryTable,
                    OrganizationDbContext.Schema))
                .EnableDetailedErrors(false)
                .EnableSensitiveDataLogging(false);
        });

        services.AddScoped<IOrganizationDataSeeder, OrganizationDataSeeder>();

        return services;
    }
}
