using HospitalManagement.Modules.Scheduling.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public sealed class DoctorScheduleEntityTypeConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("doctor_schedules", SchedulingDbContext.Schema);

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.DoctorId)
            .IsRequired()
            .HasColumnName("doctor_id");

        builder.Property(s => s.DepartmentId)
            .IsRequired()
            .HasColumnName("department_id");

        builder.Property(s => s.DayOfWeek)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasColumnName("day_of_week");

        builder.Property(s => s.StartTime)
            .IsRequired()
            .HasColumnName("start_time");

        builder.Property(s => s.EndTime)
            .IsRequired()
            .HasColumnName("end_time");

        builder.Property(s => s.SlotDurationMinutes)
            .IsRequired()
            .HasColumnName("slot_duration_minutes");

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasColumnName("is_active");

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at_utc");

        builder.Property(s => s.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(s => new { s.DoctorId, s.DayOfWeek, s.IsActive })
            .IsUnique()
            .HasFilter("\"is_active\" = TRUE")
            .HasDatabaseName("ux_doctor_schedules_active_doctor_day");

        builder.HasMany(s => s.Breaks)
            .WithOne()
            .HasForeignKey(b => b.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ScheduleBreakEntityTypeConfiguration : IEntityTypeConfiguration<ScheduleBreak>
{
    public void Configure(EntityTypeBuilder<ScheduleBreak> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("schedule_breaks", SchedulingDbContext.Schema);

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id");

        builder.Property(b => b.ScheduleId)
            .IsRequired()
            .HasColumnName("schedule_id");

        builder.Property(b => b.StartTime)
            .IsRequired()
            .HasColumnName("start_time");

        builder.Property(b => b.EndTime)
            .IsRequired()
            .HasColumnName("end_time");

        builder.Property(b => b.Reason)
            .IsRequired()
            .HasMaxLength(128)
            .HasColumnName("reason");

        builder.HasIndex(b => b.ScheduleId)
            .HasDatabaseName("ix_schedule_breaks_schedule_id");
    }
}
