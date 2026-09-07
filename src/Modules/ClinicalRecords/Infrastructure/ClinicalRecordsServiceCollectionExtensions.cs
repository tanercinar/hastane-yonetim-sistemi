using HospitalManagement.BuildingBlocks.Clinical;
using HospitalManagement.BuildingBlocks.Storage;
using HospitalManagement.Modules.ClinicalRecords.Application;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure;

public static class ClinicalRecordsServiceCollectionExtensions
{
    public static IServiceCollection AddClinicalRecordsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services.AddDbContext<ClinicalRecordsDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString(connectionStringName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    ClinicalRecordsDbContext.MigrationHistoryTable,
                    ClinicalRecordsDbContext.Schema));
        });

        services.TryAddSingleton<IBlobStorageService, InMemoryBlobStorageService>();
        services.TryAddSingleton<IAttachmentMalwareScanner, MockAttachmentMalwareScanner>();
        services.AddScoped<IClinicalRecordsDataSeeder, ClinicalRecordsDataSeeder>();
        services.AddScoped<ClinicalRecordAccessControl>();
        services.AddScoped<IEncounterService, EncounterService>();
        services.AddScoped<IEncounterReferenceLookup, EncounterReferenceLookup>();
        services.AddScoped<IAllergyProblemService, AllergyProblemService>();
        services.AddScoped<IVitalSignsService, VitalSignsService>();
        services.AddScoped<IClinicalNoteService, ClinicalNoteService>();
        services.AddScoped<IDiagnosisService, DiagnosisService>();
        services.AddScoped<IConsultationService, ConsultationService>();
        services.AddScoped<IClinicalAttachmentService, ClinicalAttachmentService>();
        services.AddScoped<IPatientTimelineService, PatientTimelineService>();
        services.AddScoped<IPatientAllergyLookup, PatientAllergyLookup>();

        return services;
    }
}
