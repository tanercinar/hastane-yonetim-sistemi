using Microsoft.Extensions.Options;

namespace HospitalManagement.Host.Configuration;

internal sealed partial class ValidatedConfigurationReporter(
    ILogger<ValidatedConfigurationReporter> logger,
    IHostEnvironment environment,
    IOptions<RuntimeOptions> runtimeOptions,
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<ObjectStorageOptions> objectStorageOptions,
    IOptions<EmailDeliveryOptions> emailOptions) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = runtimeOptions.Value;
        _ = databaseOptions.Value;
        _ = objectStorageOptions.Value;
        _ = emailOptions.Value;

        LogConfigurationValidated(logger, environment.EnvironmentName);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Configuration contract validated for environment {EnvironmentName}; critical secret values are not logged.")]
    private static partial void LogConfigurationValidated(ILogger logger, string environmentName);
}
