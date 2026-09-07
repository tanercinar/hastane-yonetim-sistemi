using HospitalManagement.Host.Configuration;

using Microsoft.EntityFrameworkCore;

using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace HospitalManagement.Host.Database;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseFoundation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName)
            ?? string.Empty;

        services.AddDbContext<DatabaseBootstrapDbContext>(options =>
            ConfigureDatabase(options, connectionString));

        return services;
    }

    internal static void ConfigureDatabase(DbContextOptionsBuilder options, string connectionString)
    {
        options
            .UseNpgsql(connectionString, ConfigurePostgreSql)
            .EnableDetailedErrors(false)
            .EnableSensitiveDataLogging(false);
    }

    private static void ConfigurePostgreSql(NpgsqlDbContextOptionsBuilder options)
    {
        options.MigrationsHistoryTable(
            DatabaseBootstrapDbContext.MigrationHistoryTable,
            DatabaseSchemas.Platform);
    }
}
