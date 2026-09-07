using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure;

public static class DiagnosticsServiceCollectionExtensions
{
    public static IServiceCollection AddDiagnosticsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services.AddDbContext<DiagnosticsDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString(connectionStringName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory_Diagnostics",
                    "diagnostics"));
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IDiagnosticOrderService, DiagnosticOrderService>();
        services.AddScoped<ILabCatalogService, LabCatalogService>();
        services.AddScoped<ILabCatalogDataSeeder, LabCatalogDataSeeder>();
        services.AddScoped<ISpecimenService, SpecimenService>();
        services.AddScoped<ICriticalResultNotificationService, CriticalResultNotificationService>();
        services.AddScoped<ILabResultService, LabResultService>();
        services.AddScoped<IRadiologyService, RadiologyService>();
        services.AddScoped<IRadiologyCatalogDataSeeder, RadiologyCatalogDataSeeder>();
        services.AddSingleton<IDicomPreviewTokenProtector, DicomPreviewTokenProtector>();
        services.AddScoped<IDicomSimulationService, DicomSimulationService>();
        services.AddScoped<IPathologyService, PathologyService>();
        services.AddScoped<IBloodBankService, BloodBankService>();
        services.AddScoped<IBloodBankDataSeeder, BloodBankDataSeeder>();
        services.AddScoped<IDiagnosticTimelineService, DiagnosticTimelineService>();

        return services;
    }
}
