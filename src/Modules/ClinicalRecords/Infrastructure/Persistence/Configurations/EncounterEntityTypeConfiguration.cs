using HospitalManagement.Modules.ClinicalRecords.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Configurations;

public sealed class EncounterEntityTypeConfiguration : IEntityTypeConfiguration<Encounter>
{
    public void Configure(EntityTypeBuilder<Encounter> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("encounters", ClinicalRecordsDbContext.Schema);

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.AppointmentId)
            .IsRequired(false)
            .HasColumnName("appointment_id");

        builder.Property(e => e.PatientId)
            .IsRequired()
            .HasColumnName("patient_id");

        builder.Property(e => e.DepartmentId)
            .IsRequired()
            .HasColumnName("department_id");

        builder.Property(e => e.PrimaryPractitionerId)
            .IsRequired()
            .HasColumnName("primary_practitioner_id");

        builder.Property(e => e.EncounterType)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("encounter_type");

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("status");

        builder.Property(e => e.PlannedStartTimeUtc)
            .IsRequired(false)
            .HasColumnName("planned_start_time_utc");

        builder.Property(e => e.ActualStartTimeUtc)
            .IsRequired(false)
            .HasColumnName("actual_start_time_utc");

        builder.Property(e => e.ActualEndTimeUtc)
            .IsRequired(false)
            .HasColumnName("actual_end_time_utc");

        builder.Property(e => e.ChiefComplaint)
            .HasMaxLength(1000)
            .IsRequired(false)
            .HasColumnName("chief_complaint");

        builder.Property(e => e.CancellationReason)
            .HasMaxLength(500)
            .IsRequired(false)
            .HasColumnName("cancellation_reason");

        builder.Property(e => e.EnteredInErrorReason)
            .HasMaxLength(500)
            .IsRequired(false)
            .HasColumnName("entered_in_error_reason");

        builder.Property(e => e.ReopenReason)
            .HasMaxLength(500)
            .IsRequired(false)
            .HasColumnName("reopen_reason");

        builder.Property(e => e.ReopenedAtUtc)
            .IsRequired(false)
            .HasColumnName("reopened_at_utc");

        builder.Property(e => e.ReopenedByPractitionerId)
            .IsRequired(false)
            .HasColumnName("reopened_by_practitioner_id");

        builder.Property(e => e.Version)
            .IsConcurrencyToken()
            .IsRequired()
            .HasColumnName("version");

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at_utc");

        builder.Property(e => e.UpdatedAtUtc)
            .IsRequired(false)
            .HasColumnName("updated_at_utc");

        builder.HasMany(e => e.Participants)
            .WithOne()
            .HasForeignKey(p => p.EncounterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Participants)
            .HasField("_participants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(e => e.PatientId)
            .HasDatabaseName("ix_encounters_patient_id");

        builder.HasIndex(e => e.DepartmentId)
            .HasDatabaseName("ix_encounters_department_id");

        builder.HasIndex(e => e.PrimaryPractitionerId)
            .HasDatabaseName("ix_encounters_primary_practitioner_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_encounters_status");

        // Unique filtered index: One active/completed encounter per appointment
        // Status 1 (Planned), 2 (InProgress), 3 (Completed)
        builder.HasIndex(e => e.AppointmentId)
            .HasDatabaseName("ux_encounters_appointment_active")
            .HasFilter("\"appointment_id\" IS NOT NULL AND \"status\" IN (1, 2, 3)")
            .IsUnique();
    }
}
