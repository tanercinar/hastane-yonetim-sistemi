using HospitalManagement.Modules.Reporting.Application;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.Modules.Reporting.Infrastructure;

public static class ReportingServiceCollectionExtensions
{
    public const string SchemaName = "reporting";

    public static IServiceCollection AddReportingModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services.AddDbContext<ReportingDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString(connectionStringName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory_Reporting",
                    SchemaName));
        });

        services.AddScoped<IReportingProjectionEngine, ReportingProjectionEngine>();
        services.AddScoped<IProjectionRebuilder, ProjectionRebuilder>();
        services.AddScoped<IReportingReadModelService, ReportingReadModelService>();
        services.AddScoped<IOutpatientDashboardService, OutpatientDashboardService>();
        services.AddScoped<IDiagnosticDashboardService, DiagnosticDashboardService>();
        services.AddScoped<IInpatientOperationsDashboardService, InpatientOperationsDashboardService>();
        services.AddScoped<IPharmacyDashboardService, PharmacyDashboardService>();
        services.AddScoped<ISecureExportService, SecureExportService>();

        return services;
    }
}
