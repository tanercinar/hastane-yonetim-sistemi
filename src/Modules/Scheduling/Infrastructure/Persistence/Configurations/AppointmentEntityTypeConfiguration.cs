using HospitalManagement.Modules.Scheduling.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public sealed class AppointmentEntityTypeConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("appointments", SchedulingDbContext.Schema);

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.SlotId)
            .IsRequired()
            .HasColumnName("slot_id");

        builder.Property(a => a.PatientId)
            .IsRequired()
            .HasColumnName("patient_id");

        builder.Property(a => a.DoctorId)
            .IsRequired()
            .HasColumnName("doctor_id");

        builder.Property(a => a.DepartmentId)
            .IsRequired()
            .HasColumnName("department_id");

        builder.Property(a => a.AppointmentTimeUtc)
            .IsRequired()
            .HasColumnName("appointment_time_utc");

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasColumnName("status");

        builder.Property(a => a.ReasonForVisit)
            .HasMaxLength(512)
            .HasColumnName("reason_for_visit");

        builder.Property(a => a.CancellationReason)
            .HasMaxLength(512)
            .HasColumnName("cancellation_reason");

        builder.Property(a => a.CancelledAtUtc)
            .HasColumnName("cancelled_at_utc");

        builder.Property(a => a.CheckedInAtUtc)
            .HasColumnName("checked_in_at_utc");

        builder.Property(a => a.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        builder.Property(a => a.QueueNumber)
            .HasColumnName("queue_number");

        builder.Property(a => a.Version)
            .IsConcurrencyToken()
            .IsRequired()
            .HasColumnName("version");

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at_utc");

        builder.Property(a => a.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(a => a.SlotId)
            .IsUnique()
            .HasFilter("\"status\" != 'Cancelled'")
            .HasDatabaseName("ix_appointments_slot_id");

        builder.HasIndex(a => new { a.PatientId, a.AppointmentTimeUtc })
            .HasDatabaseName("ix_appointments_patient_time");

        builder.HasIndex(a => new { a.DoctorId, a.AppointmentTimeUtc, a.Status })
            .HasDatabaseName("ix_appointments_doctor_time_status");

        builder.HasIndex(a => new { a.DepartmentId, a.AppointmentTimeUtc, a.Status })
            .HasDatabaseName("ix_appointments_dept_time_status");
    }
}
