using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;

public static class SurgeryServiceCollectionExtensions
{
    public const string SchemaName = "surgery";

    public static IServiceCollection AddSurgeryCriticalCareModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services.AddDbContext<SurgeryDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString(connectionStringName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory_Surgery",
                    SchemaName));
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<ISurgeryDataSeeder, SurgeryDataSeeder>();
        services.AddScoped<ISurgeryPlanningService, SurgeryPlanningService>();
        services.AddScoped<IPerioperativeRecordService, PerioperativeRecordService>();
        services.AddScoped<IIcuAdmissionService, IcuAdmissionService>();
        services.AddScoped<IIcuFlowsheetService, IcuFlowsheetService>();
        services.AddScoped<IClinicalHandoffService, ClinicalHandoffService>();

        return services;
    }
}
