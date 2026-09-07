using System.Threading.RateLimiting;

using HospitalManagement.Host.Configuration;
using HospitalManagement.Host.Health;
using HospitalManagement.Modules.IdentityAccess.Infrastructure;

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HospitalManagement.Host.Api;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiFoundation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<ApiRateLimitOptions>()
            .Bind(configuration.GetRequiredSection(ApiRateLimitOptions.SectionName))
            .Validate(
                options => options.PermitLimit is >= 1 and <= 10_000,
                $"'{ApiRateLimitOptions.SectionName}:PermitLimit' must be between 1 and 10000.")
            .Validate(
                options => options.WindowSeconds is >= 1 and <= 3_600,
                $"'{ApiRateLimitOptions.SectionName}:WindowSeconds' must be between 1 and 3600.")
            .ValidateOnStart();

        services.AddOpenApi(ApiConstants.Version);
        services.AddValidation();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ApiProblemDetailsDefaults.Customize;
        });
        services.AddExceptionHandler<ApiExceptionHandler>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = ApiRateLimitRejectionHandler.HandleAsync;
            options.AddPolicy(ApiConstants.RateLimitPolicyName, httpContext =>
            {
                var configuredRateLimit = httpContext.RequestServices
                    .GetRequiredService<IOptions<ApiRateLimitOptions>>()
                    .Value;

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ApiConstants.RateLimitPolicyName,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuredRateLimit.PermitLimit,
                        Window = TimeSpan.FromSeconds(configuredRateLimit.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    });
            });
            options.AddPolicy(IdentityAccessConstants.RateLimitPolicyName, httpContext =>
            {
                var configured = httpContext.RequestServices
                    .GetRequiredService<IOptions<IdentityAccessOptions>>()
                    .Value;
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown-client";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configured.SensitivePermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    });
            });
        });

        services
            .AddHealthChecks()
            .AddCheck<PostgreSqlReadinessHealthCheck>(
                "postgresql",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ApiConstants.ReadinessTag]);

        return services;
    }
}
