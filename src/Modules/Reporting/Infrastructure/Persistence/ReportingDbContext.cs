using HospitalManagement.Modules.Reporting.Domain;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Reporting.Infrastructure.Persistence;

public sealed class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options)
        : base(options)
    {
    }

    public DbSet<DailyOutpatientMetric> DailyOutpatientMetrics => Set<DailyOutpatientMetric>();
    public DbSet<DiagnosticWorkloadMetric> DiagnosticWorkloadMetrics => Set<DiagnosticWorkloadMetric>();
    public DbSet<BedOccupancyMetric> BedOccupancyMetrics => Set<BedOccupancyMetric>();
    public DbSet<PharmacyDispensingMetric> PharmacyDispensingMetrics => Set<PharmacyDispensingMetric>();
    public DbSet<ProjectionCheckpoint> ProjectionCheckpoints => Set<ProjectionCheckpoint>();
    public DbSet<ProjectionProcessedEvent> ProjectionProcessedEvents => Set<ProjectionProcessedEvent>();
    public DbSet<ProjectionSourceEvent> ProjectionSourceEvents => Set<ProjectionSourceEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reporting");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
