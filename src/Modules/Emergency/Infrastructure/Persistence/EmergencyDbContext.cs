using HospitalManagement.Modules.Emergency.Domain;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Emergency.Infrastructure.Persistence;

public sealed class EmergencyDbContext : DbContext
{
    public const string SchemaName = "emergency";

    public EmergencyDbContext(DbContextOptions<EmergencyDbContext> options)
        : base(options)
    {
    }

    public DbSet<EmergencyAdmission> Admissions => Set<EmergencyAdmission>();
    public DbSet<EmergencyCareOrder> Orders => Set<EmergencyCareOrder>();
    public DbSet<EmergencyConsultation> Consultations => Set<EmergencyConsultation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmergencyDbContext).Assembly);
    }
}
