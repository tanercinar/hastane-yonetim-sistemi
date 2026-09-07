using HospitalManagement.BuildingBlocks.Audit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;

public static class AuditPrivacyPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddAuditPrivacyPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<AuditPrivacyDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AuditPrivacyDbContext.SchemaName)));

        services.AddScoped<AuditLogService>();
        services.AddScoped<IAuditEventPublisher>(sp => sp.GetRequiredService<AuditLogService>());
        services.AddScoped<IAuditLogReader>(sp => sp.GetRequiredService<AuditLogService>());

        return services;
    }

    public static IServiceCollection AddAuditPrivacyPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services.AddDbContext<AuditPrivacyDbContext>((serviceProvider, options) =>
        {
            var resolvedConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = resolvedConfiguration.GetConnectionString(connectionStringName)
                ?? configuration.GetConnectionString(connectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' was not found.");

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AuditPrivacyDbContext.SchemaName));
        });

        services.AddScoped<AuditLogService>();
        services.AddScoped<IAuditEventPublisher>(sp => sp.GetRequiredService<AuditLogService>());
        services.AddScoped<IAuditLogReader>(sp => sp.GetRequiredService<AuditLogService>());

        return services;
    }
}
