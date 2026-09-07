using HospitalManagement.BuildingBlocks.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Domain;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Patients.Infrastructure.Persistence;

public sealed class PatientsDbContext(DbContextOptions<PatientsDbContext> options)
    : ModuleDbContext(options)
{
    public const string Schema = "patients";
    public const string MigrationHistoryTable = "__ef_migrations_history";

    public DbSet<Patient> Patients => Set<Patient>();

    protected override void ConfigureModuleModel(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientsDbContext).Assembly);
    }
}
