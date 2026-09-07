using HospitalManagement.Modules.Scheduling.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public sealed class DoctorLeaveBlockEntityTypeConfiguration : IEntityTypeConfiguration<DoctorLeaveBlock>
{
    public void Configure(EntityTypeBuilder<DoctorLeaveBlock> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("doctor_leave_blocks", SchedulingDbContext.Schema);

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id");

        builder.Property(l => l.DoctorId)
            .IsRequired()
            .HasColumnName("doctor_id");

        builder.Property(l => l.StartUtc)
            .IsRequired()
            .HasColumnName("start_utc");

        builder.Property(l => l.EndUtc)
            .IsRequired()
            .HasColumnName("end_utc");

        builder.Property(l => l.Reason)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("reason");

        builder.Property(l => l.IsActive)
            .IsRequired()
            .HasColumnName("is_active");

        builder.Property(l => l.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at_utc");

        builder.HasIndex(l => new { l.DoctorId, l.StartUtc, l.EndUtc, l.IsActive })
            .HasDatabaseName("ix_doctor_leave_blocks_doctor_dates");
    }
}
