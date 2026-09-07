using HospitalManagement.Host.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HospitalManagement.Host.Database;

public sealed class DatabaseBootstrapDbContextFactory
    : IDesignTimeDbContextFactory<DatabaseBootstrapDbContext>
{
    public DatabaseBootstrapDbContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environments.Development;
        var configurationBasePath = FindConfigurationBasePath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(configurationBasePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddUserSecrets<DatabaseBootstrapDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Migration configuration requires 'ConnectionStrings:{DatabaseOptions.ConnectionStringName}'.");
        }

        var options = new DbContextOptionsBuilder<DatabaseBootstrapDbContext>();
        DatabaseServiceCollectionExtensions.ConfigureDatabase(options, connectionString);
        return new DatabaseBootstrapDbContext(options.Options);
    }

    private static string FindConfigurationBasePath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            currentDirectory,
            Path.Combine(currentDirectory, "src", "HospitalManagement.Host"),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "appsettings.json")))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "Could not locate HospitalManagement.Host/appsettings.json for migration tooling.");
    }
}
