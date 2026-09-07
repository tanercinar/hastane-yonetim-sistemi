using System.Data.Common;
using System.Net.Mail;

namespace HospitalManagement.Host.Configuration;

public static class ConfigurationServiceCollectionExtensions
{
    private static readonly string[] SupportedEnvironmentNames =
    [
        Environments.Development,
        "Testing",
        Environments.Production,
    ];

    public static IServiceCollection AddValidatedHospitalManagementConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        ValidateEnvironmentName(environment.EnvironmentName);

        services
            .AddOptions<RuntimeOptions>()
            .Bind(configuration.GetRequiredSection(RuntimeOptions.SectionName))
            .Validate(
                options => string.Equals(options.DataMode, "DEMO", StringComparison.Ordinal),
                $"Critical configuration '{RuntimeOptions.SectionName}:DataMode' must be DEMO.")
            .Validate(
                options => !options.EmbeddedArtificialIntelligenceEnabled,
                $"Critical configuration '{RuntimeOptions.SectionName}:EmbeddedArtificialIntelligenceEnabled' must be false; embedded AI is out of scope.")
            .ValidateOnStart();

        services
            .AddOptions<DatabaseOptions>()
            .Configure(options =>
            {
                options.ConnectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName)
                    ?? string.Empty;
            })
            .Validate(
                HasValidDatabaseConnectionString,
                $"Missing or invalid critical configuration 'ConnectionStrings:{DatabaseOptions.ConnectionStringName}'. It must contain Host, Database, Username, and Password.")
            .ValidateOnStart();

        services
            .AddOptions<ObjectStorageOptions>()
            .Bind(configuration.GetRequiredSection(ObjectStorageOptions.SectionName))
            .Validate(
                options => HasValidObjectStorageEndpoint(options, environment),
                $"Critical configuration '{ObjectStorageOptions.SectionName}:Endpoint' must be an absolute HTTP(S) endpoint; Production requires HTTPS and UseTls=true.")
            .Validate(
                options => options.AccessKey.Length >= 3,
                $"Missing critical secret '{ObjectStorageOptions.SectionName}:AccessKey'.")
            .Validate(
                options => options.SecretKey.Length >= 8,
                $"Missing or invalid critical secret '{ObjectStorageOptions.SectionName}:SecretKey'.")
            .Validate(
                options => HasValidBucketName(options.BucketName),
                $"Critical configuration '{ObjectStorageOptions.SectionName}:BucketName' must be a lowercase DNS-compatible name.")
            .ValidateOnStart();

        services
            .AddOptions<EmailDeliveryOptions>()
            .Bind(configuration.GetRequiredSection(EmailDeliveryOptions.SectionName))
            .Validate(
                options => string.Equals(options.Mode, "MOCK", StringComparison.Ordinal),
                $"Critical configuration '{EmailDeliveryOptions.SectionName}:Mode' must be MOCK; real delivery is out of scope.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Host),
                $"Missing critical configuration '{EmailDeliveryOptions.SectionName}:Host'.")
            .Validate(
                options => options.Port is >= 1 and <= 65_535,
                $"Critical configuration '{EmailDeliveryOptions.SectionName}:Port' must be between 1 and 65535.")
            .Validate(
                options => HasReservedDemoSender(options.SenderAddress),
                $"Critical configuration '{EmailDeliveryOptions.SectionName}:SenderAddress' must use the reserved .invalid domain.")
            .ValidateOnStart();

        services.AddHostedService<ValidatedConfigurationReporter>();

        return services;
    }

    private static void ValidateEnvironmentName(string environmentName)
    {
        if (SupportedEnvironmentNames.Contains(environmentName, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported ASPNETCORE_ENVIRONMENT '{environmentName}'. Supported values: Development, Testing, Production.");
    }

    private static bool HasValidDatabaseConnectionString(DatabaseOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return false;
        }

        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = options.ConnectionString,
            };

            return HasNonEmptyValue(builder, "Host")
                && HasNonEmptyValue(builder, "Database")
                && HasNonEmptyValue(builder, "Username")
                && HasNonEmptyValue(builder, "Password");
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool HasNonEmptyValue(DbConnectionStringBuilder builder, string key)
    {
        return builder.TryGetValue(key, out var value)
            && !string.IsNullOrWhiteSpace(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static bool HasValidObjectStorageEndpoint(ObjectStorageOptions options, IHostEnvironment environment)
    {
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        var endpointUsesTls = endpoint.Scheme == Uri.UriSchemeHttps;
        if (options.UseTls != endpointUsesTls)
        {
            return false;
        }

        if (environment.IsProduction())
        {
            return endpointUsesTls;
        }

        return endpointUsesTls || endpoint.IsLoopback;
    }

    private static bool HasValidBucketName(string bucketName)
    {
        if (bucketName.Length is < 3 or > 63
            || bucketName[0] == '-'
            || bucketName[^1] == '-')
        {
            return false;
        }

        return bucketName.All(character =>
            (character is >= 'a' and <= 'z')
            || char.IsAsciiDigit(character)
            || character == '-');
    }

    private static bool HasReservedDemoSender(string senderAddress)
    {
        try
        {
            var parsedAddress = new MailAddress(senderAddress);
            return string.Equals(parsedAddress.Address, senderAddress, StringComparison.Ordinal)
                && parsedAddress.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
