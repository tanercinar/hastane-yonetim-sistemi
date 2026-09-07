using HospitalManagement.Modules.Interoperability.Application;
using HospitalManagement.Modules.Interoperability.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.Modules.Interoperability.Infrastructure;

public static class InteroperabilityServiceCollectionExtensions
{
    public const string SchemaName = "interoperability";

    public static IServiceCollection AddInteroperabilityModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services.AddDbContext<InteroperabilityDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString(connectionStringName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory_Interoperability",
                    SchemaName));
        });

        services.AddScoped<IIntegrationMockEngine, IntegrationMockEngine>();
        services.AddScoped<IFhirR4Service, FhirR4Service>();
        services.AddScoped<IHl7V2Service, Hl7V2Service>();
        services.AddScoped<IDicomPacsService, DicomPacsService>();
        services.AddScoped<IMhrsService, MhrsService>();
        services.AddScoped<IENabizService, ENabizService>();
        services.AddScoped<IMedulaBoundaryService, MedulaBoundaryService>();

        return services;
    }
}
