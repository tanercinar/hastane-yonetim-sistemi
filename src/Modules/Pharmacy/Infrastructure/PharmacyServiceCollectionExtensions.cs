using HospitalManagement.Modules.Pharmacy.Application;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.Modules.Pharmacy.Infrastructure;

public static class PharmacyServiceCollectionExtensions
{
    public static IServiceCollection AddPharmacyModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services.AddDbContext<PharmacyDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString(connectionStringName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory_Pharmacy",
                    PharmacyDbContext.SchemaName));
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IMedicationCatalogDataSeeder, MedicationCatalogDataSeeder>();
        services.AddScoped<IMedicationCatalogService, MedicationCatalogService>();
        services.AddScoped<IMedicationSafetyChecker, MedicationSafetyChecker>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IMedicationStockService, MedicationStockService>();

        return services;
    }
}
