using HospitalManagement.Host.Configuration;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using Npgsql;

namespace HospitalManagement.Host.Health;

public sealed class PostgreSqlReadinessHealthCheck(IOptions<DatabaseOptions> databaseOptions) : IHealthCheck
{
    private const int MaximumConnectionTimeoutSeconds = 3;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(
                databaseOptions.Value.ConnectionString);
            connectionStringBuilder.Timeout = Math.Clamp(
                connectionStringBuilder.Timeout,
                1,
                MaximumConnectionTimeoutSeconds);
            connectionStringBuilder.CommandTimeout = MaximumConnectionTimeoutSeconds;

            await using var connection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1;", connection)
            {
                CommandTimeout = MaximumConnectionTimeoutSeconds,
            };
            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result is int value && value == 1
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch (Exception exception) when (IsDependencyFailure(exception, cancellationToken))
        {
            return HealthCheckResult.Unhealthy();
        }
    }

    private static bool IsDependencyFailure(Exception exception, CancellationToken cancellationToken)
    {
        return exception is NpgsqlException
            or ArgumentException
            or InvalidOperationException
            or TimeoutException
            || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested;
    }
}
