using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.Notifications.Application;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace HospitalManagement.IntegrationTests.Infrastructure;

internal sealed class ApiWebApplicationFactory(
    string databaseConnectionString,
    int permitLimit = 120,
    ILoggerProvider? loggerProvider = null,
    IIdentityMessageSender? identityMessageSender = null,
    INotificationTransport? notificationTransport = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddInMemoryCollection(
                CreateSettings(databaseConnectionString, permitLimit));
        });

        if (loggerProvider is not null)
        {
            builder.ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.AddProvider(loggerProvider);
            });
        }

        if (identityMessageSender is not null)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IIdentityMessageSender>();
                services.AddSingleton(identityMessageSender);
            });
        }

        if (notificationTransport is not null)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<INotificationTransport>();
                services.AddSingleton(notificationTransport);
            });
        }
    }

    private static Dictionary<string, string?> CreateSettings(
        string databaseConnectionString,
        int permitLimit)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["HospitalManagement:Runtime:DataMode"] = "DEMO",
            ["HospitalManagement:Runtime:EmbeddedArtificialIntelligenceEnabled"] = "false",
            ["HospitalManagement:Api:RateLimit:PermitLimit"] = permitLimit.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            ["HospitalManagement:Api:RateLimit:WindowSeconds"] = "60",
            ["HospitalManagement:Identity:SensitivePermitLimit"] = "100",
            ["ConnectionStrings:HospitalDatabase"] = databaseConnectionString,
            ["HospitalManagement:ObjectStorage:Endpoint"] = "http://127.0.0.1:9000",
            ["HospitalManagement:ObjectStorage:UseTls"] = "false",
            ["HospitalManagement:ObjectStorage:BucketName"] = "demo-api-contract-documents",
            ["HospitalManagement:ObjectStorage:AccessKey"] = "DEMO-API-TEST-ACCESS",
            ["HospitalManagement:ObjectStorage:SecretKey"] = "DEMO-API-TEST-SECRET",
            ["HospitalManagement:Email:Mode"] = "MOCK",
            ["HospitalManagement:Email:Host"] = "127.0.0.1",
            ["HospitalManagement:Email:Port"] = "1025",
            ["HospitalManagement:Email:SenderAddress"] = "DEMO-api-test@hospital.invalid",
        };
    }
}
