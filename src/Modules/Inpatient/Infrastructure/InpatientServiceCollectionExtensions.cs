using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.Modules.Inpatient.Infrastructure;

public static class InpatientServiceCollectionExtensions
{
    public const string SchemaName = "inpatient";

    public static IServiceCollection AddInpatientModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services.AddDbContext<InpatientDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString(connectionStringName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory_Inpatient",
                    SchemaName));
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IInpatientDataSeeder, InpatientDataSeeder>();
        services.AddScoped<IBedManagementService, BedManagementService>();
        services.AddScoped<IInpatientAdmissionService, InpatientAdmissionService>();
        services.AddScoped<IInpatientTransferService, InpatientTransferService>();
        services.AddScoped<IInpatientBoardService, InpatientBoardService>();
        services.AddScoped<INursingCareService, NursingCareService>();
        services.AddScoped<IInpatientMedicationOrderValidator, RejectingInpatientMedicationOrderValidator>();
        services.AddScoped<IMedicationAdministrationService, MedicationAdministrationService>();
        services.AddScoped<IInpatientDischargeService, InpatientDischargeService>();
        services.AddScoped<IInpatientDashboardService, InpatientDashboardService>();
        services.AddScoped<IInpatientRealtimeNotifier, NoOpInpatientRealtimeNotifier>();

        return services;
    }
}
