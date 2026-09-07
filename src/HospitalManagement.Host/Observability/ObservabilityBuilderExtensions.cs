using System.Text.Json;

using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HospitalManagement.Host.Observability;

public static class ObservabilityBuilderExtensions
{
    public static WebApplicationBuilder AddObservabilityFoundation(
        this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var section = builder.Configuration.GetRequiredSection(ObservabilityOptions.SectionName);
        var settings = section.Get<ObservabilityOptions>()
            ?? throw new OptionsValidationException(
                ObservabilityOptions.SectionName,
                typeof(ObservabilityOptions),
                ["Observability configuration is required."]);

        builder.Services
            .AddOptions<ObservabilityOptions>()
            .Bind(section)
            .Validate(
                options => IsSafeServiceName(options.ServiceName),
                $"'{ObservabilityOptions.SectionName}:ServiceName' must contain only letters, digits, '.', '-' or '_'.")
            .Validate(
                options => !builder.Environment.IsProduction() || !options.ConsoleExporterEnabled,
                $"'{ObservabilityOptions.SectionName}:ConsoleExporterEnabled' must be false in Production.")
            .ValidateOnStart();

        builder.Logging.ClearProviders();
        builder.Logging.Configure(options =>
        {
            options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId
                | ActivityTrackingOptions.SpanId
                | ActivityTrackingOptions.ParentId;
        });
        builder.Logging.AddJsonConsole(options =>
        {
            // ASP.NET request scopes contain the raw RequestPath. Correlation and trace
            // identifiers are emitted explicitly by RequestTelemetryMiddleware instead.
            options.IncludeScopes = false;
            options.UseUtcTimestamp = true;
            options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
            options.JsonWriterOptions = new JsonWriterOptions
            {
                Indented = false,
            };
        });
        builder.Logging.EnableRedaction();
        builder.Services.AddRedaction();

        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(settings.ServiceName));

        openTelemetry.WithTracing(tracing =>
        {
            tracing
                .SetSampler(new AlwaysOnSampler())
                .AddSource(ObservabilityTelemetry.ActivitySourceName);
            if (settings.ConsoleExporterEnabled)
            {
                tracing.AddConsoleExporter();
            }
        });

        openTelemetry.WithMetrics(metrics =>
        {
            metrics.AddMeter(ObservabilityTelemetry.MeterName);
            if (settings.ConsoleExporterEnabled)
            {
                metrics.AddConsoleExporter((_, readerOptions) =>
                {
                    readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5_000;
                });
            }
        });

        return builder;
    }

    private static bool IsSafeServiceName(string serviceName)
    {
        return !string.IsNullOrWhiteSpace(serviceName)
            && serviceName.Length <= 64
            && serviceName.All(character =>
                char.IsAsciiLetterOrDigit(character)
                || character is '.' or '-' or '_');
    }
}
