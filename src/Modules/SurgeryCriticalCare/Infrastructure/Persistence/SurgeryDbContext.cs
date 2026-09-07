using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;

public sealed class SurgeryDbContext : DbContext
{
    public const string SchemaName = "surgery";

    public SurgeryDbContext(DbContextOptions<SurgeryDbContext> options)
        : base(options)
    {
    }

    public DbSet<OperatingRoom> OperatingRooms => Set<OperatingRoom>();
    public DbSet<SurgeryBooking> Bookings => Set<SurgeryBooking>();
    public DbSet<PerioperativeRecord> PerioperativeRecords => Set<PerioperativeRecord>();
    public DbSet<PerioperativeCorrection> PerioperativeCorrections => Set<PerioperativeCorrection>();
    public DbSet<IcuBed> IcuBeds => Set<IcuBed>();
    public DbSet<IcuAdmission> IcuAdmissions => Set<IcuAdmission>();
    public DbSet<IcuFlowsheetEntry> IcuFlowsheetEntries => Set<IcuFlowsheetEntry>();
    public DbSet<ClinicalHandoff> ClinicalHandoffs => Set<ClinicalHandoff>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SurgeryDbContext).Assembly);
    }
}
