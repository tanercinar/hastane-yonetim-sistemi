using HospitalManagement.BuildingBlocks.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Domain;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Organization.Infrastructure.Persistence;

public sealed class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
    : ModuleDbContext(options)
{
    public const string Schema = "organization";
    public const string MigrationHistoryTable = "__ef_migrations_history";

    public DbSet<Hospital> Hospitals => Set<Hospital>();

    public DbSet<Facility> Facilities => Set<Facility>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Specialty> Specialties => Set<Specialty>();

    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();

    public DbSet<StaffDepartmentAssignment> StaffDepartmentAssignments =>
        Set<StaffDepartmentAssignment>();

    protected override void ConfigureModuleModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationDbContext).Assembly);
        OrganizationDemoData.Configure(modelBuilder);
    }
}
