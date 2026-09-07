using HospitalManagement.Modules.Inpatient.Domain;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;

public sealed class InpatientDbContext : DbContext
{
    public InpatientDbContext(DbContextOptions<InpatientDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Bed> Beds => Set<Bed>();
    public DbSet<InpatientAdmission> Admissions => Set<InpatientAdmission>();
    public DbSet<InpatientTransfer> Transfers => Set<InpatientTransfer>();
    public DbSet<NursingObservation> NursingObservations => Set<NursingObservation>();
    public DbSet<NursingCarePlan> NursingCarePlans => Set<NursingCarePlan>();
    public DbSet<NursingCareTask> NursingCareTasks => Set<NursingCareTask>();
    public DbSet<MedicationAdministration> MedicationAdministrations => Set<MedicationAdministration>();
    public DbSet<InpatientDischarge> Discharges => Set<InpatientDischarge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inpatient");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InpatientDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
