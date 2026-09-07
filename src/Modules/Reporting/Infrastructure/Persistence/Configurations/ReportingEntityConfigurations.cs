using HospitalManagement.Modules.Reporting.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Reporting.Infrastructure.Persistence.Configurations;

public sealed class ProjectionCheckpointConfiguration : IEntityTypeConfiguration<ProjectionCheckpoint>
{
    public void Configure(EntityTypeBuilder<ProjectionCheckpoint> builder)
    {
        builder.ToTable("projection_checkpoints", "reporting");

        builder.HasKey(c => c.ProjectionName);

        builder.Property(c => c.ProjectionName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.LastError)
            .HasMaxLength(2000);
    }
}

public sealed class ProjectionProcessedEventConfiguration : IEntityTypeConfiguration<ProjectionProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProjectionProcessedEvent> builder)
    {
        builder.ToTable("projection_processed_events", "reporting");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.ProjectionName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => new { e.ProjectionName, e.ProcessedAtUtc });
    }
}

public sealed class ProjectionSourceEventConfiguration : IEntityTypeConfiguration<ProjectionSourceEvent>
{
    public void Configure(EntityTypeBuilder<ProjectionSourceEvent> builder)
    {
        builder.ToTable("projection_source_events", "reporting");

        builder.HasKey(sourceEvent => sourceEvent.Position);
        builder.Property(sourceEvent => sourceEvent.Position)
            .ValueGeneratedOnAdd();

        builder.Property(sourceEvent => sourceEvent.ProjectionName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sourceEvent => sourceEvent.EventType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(sourceEvent => sourceEvent.PayloadJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(sourceEvent => sourceEvent.EventId)
            .IsUnique();
        builder.HasIndex(sourceEvent => new { sourceEvent.ProjectionName, sourceEvent.Position });
    }
}

public sealed class DailyOutpatientMetricConfiguration : IEntityTypeConfiguration<DailyOutpatientMetric>
{
    public void Configure(EntityTypeBuilder<DailyOutpatientMetric> builder)
    {
        builder.ToTable("daily_outpatient_metrics", "reporting");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.DepartmentName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(m => m.DoctorName)
            .HasMaxLength(150);

        builder.HasIndex(m => new { m.Date, m.DepartmentId, m.DoctorId })
            .IsUnique();

        builder.HasIndex(m => m.Date);
        builder.HasIndex(m => m.DepartmentId);
    }
}

public sealed class DiagnosticWorkloadMetricConfiguration : IEntityTypeConfiguration<DiagnosticWorkloadMetric>
{
    public void Configure(EntityTypeBuilder<DiagnosticWorkloadMetric> builder)
    {
        builder.ToTable("diagnostic_workload_metrics", "reporting");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ModalityOrSection)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(m => new { m.Date, m.ModalityOrSection })
            .IsUnique();

        builder.HasIndex(m => m.Date);
    }
}

public sealed class BedOccupancyMetricConfiguration : IEntityTypeConfiguration<BedOccupancyMetric>
{
    public void Configure(EntityTypeBuilder<BedOccupancyMetric> builder)
    {
        builder.ToTable("bed_occupancy_metrics", "reporting");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.DepartmentName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(m => m.WardType)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(m => new { m.Date, m.DepartmentId, m.WardType })
            .IsUnique();

        builder.HasIndex(m => m.Date);
        builder.HasIndex(m => m.DepartmentId);
    }
}

public sealed class PharmacyDispensingMetricConfiguration : IEntityTypeConfiguration<PharmacyDispensingMetric>
{
    public void Configure(EntityTypeBuilder<PharmacyDispensingMetric> builder)
    {
        builder.ToTable("pharmacy_dispensing_metrics", "reporting");

        builder.HasKey(m => m.Id);

        builder.HasIndex(m => m.Date)
            .IsUnique();
    }
}
