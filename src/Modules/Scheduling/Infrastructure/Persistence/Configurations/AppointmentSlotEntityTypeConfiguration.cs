using HospitalManagement.Modules.Scheduling.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public sealed class AppointmentSlotEntityTypeConfiguration : IEntityTypeConfiguration<AppointmentSlot>
{
    public void Configure(EntityTypeBuilder<AppointmentSlot> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("appointment_slots", SchedulingDbContext.Schema);

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.DoctorId)
            .IsRequired()
            .HasColumnName("doctor_id");

        builder.Property(s => s.DepartmentId)
            .IsRequired()
            .HasColumnName("department_id");

        builder.Property(s => s.ScheduleId)
            .HasColumnName("schedule_id");

        builder.Property(s => s.StartUtc)
            .IsRequired()
            .HasColumnName("start_utc");

        builder.Property(s => s.EndUtc)
            .IsRequired()
            .HasColumnName("end_utc");

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasColumnName("status");

        builder.Property(s => s.HoldExpirationUtc)
            .HasColumnName("hold_expiration_utc");

        builder.Property(s => s.HeldByPersonId)
            .HasColumnName("held_by_person_id");

        builder.Property(s => s.Version)
            .IsConcurrencyToken()
            .IsRequired()
            .HasColumnName("version");

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at_utc");

        builder.Property(s => s.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        // Unique index to prevent duplicate/overlapping slots for the same doctor at the same time
        builder.HasIndex(s => new { s.DoctorId, s.StartUtc })
            .IsUnique()
            .HasDatabaseName("ux_appointment_slots_doctor_start");

        builder.HasIndex(s => new { s.DepartmentId, s.StartUtc, s.Status })
            .HasDatabaseName("ix_appointment_slots_dept_start_status");

        builder.HasIndex(s => new { s.DoctorId, s.StartUtc, s.Status })
            .HasDatabaseName("ix_appointment_slots_doctor_start_status");
    }
}
