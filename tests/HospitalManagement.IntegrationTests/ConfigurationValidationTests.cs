using System.Collections.Concurrent;
using HospitalManagement.Host.Configuration;
using HospitalManagement.Host.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HospitalManagement.IntegrationTests;

public sealed class ConfigurationValidationTests
{
    private const string TestingEnvironmentName = "Testing";
    private const string DatabaseCanary = "DEMO-SENSITIVE-CANARY-database";
    private const string AccessKeyCanary = "DEMO-SENSITIVE-CANARY-access";
    private const string SecretKeyCanary = "DEMO-SENSITIVE-CANARY-object-secret";

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F01-G05")]
    public async Task StartAsyncWithValidTestingConfigurationStartsWithoutLoggingSecrets()
    {
        using var loggerProvider = new CapturingLoggerProvider();
        using var host = CreateHostBuilder(CreateValidSettings(), TestingEnvironmentName, loggerProvider).Build();

        await host.StartAsync(CancellationToken.None);
        await host.StopAsync(CancellationToken.None);

        var logOutput = string.Join(Environment.NewLine, loggerProvider.Messages);
        Assert.Contains("Configuration contract validated for environment Testing", logOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(DatabaseCanary, logOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessKeyCanary, logOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(SecretKeyCanary, logOutput, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F01-G05")]
    public async Task StartAsyncWithMissingDatabaseConnectionStopsWithoutExposingSecretValues()
    {
        var settings = CreateValidSettings();
        settings.Remove("ConnectionStrings:HospitalDatabase");
        using var host = CreateHostBuilder(settings, TestingEnvironmentName).Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(CancellationToken.None));

        Assert.Contains("ConnectionStrings:HospitalDatabase", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessKeyCanary, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SecretKeyCanary, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F01-G05")]
    public async Task StartAsyncWithInsecureProductionObjectStorageStopsWithClearKeyName()
    {
        var settings = CreateValidSettings();
        using var host = CreateHostBuilder(settings, Environments.Production).Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(CancellationToken.None));

        Assert.Contains("HospitalManagement:ObjectStorage:Endpoint", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SecretKeyCanary, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F01-G05")]
    public async Task StartAsyncWithSecureProductionLikeConfigurationStartsSuccessfully()
    {
        var settings = CreateValidSettings();
        settings["HospitalManagement:ObjectStorage:Endpoint"] = "https://minio.demo.invalid";
        settings["HospitalManagement:ObjectStorage:UseTls"] = "true";
        using var host = CreateHostBuilder(settings, Environments.Production).Build();

        await host.StartAsync(CancellationToken.None);
        await host.StopAsync(CancellationToken.None);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F01-G05")]
    public void BuildWithUnsupportedEnvironmentStopsWithSupportedEnvironmentList()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateHostBuilder(CreateValidSettings(), "Staging").Build());

        Assert.Contains("Unsupported ASPNETCORE_ENVIRONMENT 'Staging'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Development, Testing, Production", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SecretKeyCanary, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Roadmap", "F01-G09")]
    public void ProductionConsoleTelemetryExporterIsRejected()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
        });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HospitalManagement:Observability:ServiceName"] = "HospitalManagement.Host",
            ["HospitalManagement:Observability:ConsoleExporterEnabled"] = "true",
        });
        builder.AddObservabilityFoundation();
        using var app = builder.Build();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            app.Services.GetRequiredService<IOptions<ObservabilityOptions>>().Value);

        Assert.Contains(
            "HospitalManagement:Observability:ConsoleExporterEnabled",
            exception.Message,
            StringComparison.Ordinal);
    }

    private static IHostBuilder CreateHostBuilder(
        Dictionary<string, string?> settings,
        string environmentName,
        ILoggerProvider? loggerProvider = null)
    {
        return new HostBuilder()
            .UseEnvironment(environmentName)
            .ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.Sources.Clear();
                configurationBuilder.AddInMemoryCollection(settings);
            })
            .ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                if (loggerProvider is not null)
                {
                    loggingBuilder.AddProvider(loggerProvider);
                }
            })
            .ConfigureServices((context, services) =>
            {
                services.AddValidatedHospitalManagementConfiguration(
                    context.Configuration,
                    context.HostingEnvironment);
            });
    }

    private static Dictionary<string, string?> CreateValidSettings()
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["HospitalManagement:Runtime:DataMode"] = "DEMO",
            ["HospitalManagement:Runtime:EmbeddedArtificialIntelligenceEnabled"] = "false",
            ["ConnectionStrings:HospitalDatabase"] = $"Host=127.0.0.1;Port=5432;Database=demo_test;Username=demo_test;Password={DatabaseCanary}",
            ["HospitalManagement:ObjectStorage:Endpoint"] = "http://127.0.0.1:9000",
            ["HospitalManagement:ObjectStorage:UseTls"] = "false",
            ["HospitalManagement:ObjectStorage:BucketName"] = "demo-test-documents",
            ["HospitalManagement:ObjectStorage:AccessKey"] = AccessKeyCanary,
            ["HospitalManagement:ObjectStorage:SecretKey"] = SecretKeyCanary,
            ["HospitalManagement:Email:Mode"] = "MOCK",
            ["HospitalManagement:Email:Host"] = "127.0.0.1",
            ["HospitalManagement:Email:Port"] = "1025",
            ["HospitalManagement:Email:SenderAddress"] = "DEMO-test@hospital.invalid",
        };
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<string> Messages { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

        public void Dispose()
        {
        }
    }

    private sealed class CapturingLogger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            messages.Enqueue(formatter(state, exception));
        }
    }
}
