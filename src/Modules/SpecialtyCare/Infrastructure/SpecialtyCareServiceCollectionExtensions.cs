using HospitalManagement.Modules.SpecialtyCare.Application;
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure;

public static class SpecialtyCareServiceCollectionExtensions
{
    public const string SchemaName = "specialty";

    public static IServiceCollection AddSpecialtyCareModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services.AddDbContext<SpecialtyCareDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString(connectionStringName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory_Specialty",
                    SchemaName));
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IPregnancyTrackingService, PregnancyTrackingService>();
        services.AddScoped<IDeliveryRecordService, DeliveryRecordService>();
        services.AddScoped<IDentalCareService, DentalCareService>();
        services.AddScoped<IHomeHealthCareService, HomeHealthCareService>();
        services.AddScoped<ISpecialtyReportingService, SpecialtyReportingService>();
        services.AddScoped<IPatientSpecialtyPortalService, PatientSpecialtyPortalService>();

        return services;
    }
}
