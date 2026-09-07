using HospitalManagement.Modules.ClinicalRecords.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Configurations;

public sealed class VitalSignObservationEntityTypeConfiguration : IEntityTypeConfiguration<VitalSignObservation>
{
    public void Configure(EntityTypeBuilder<VitalSignObservation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("vital_sign_observations", ClinicalRecordsDbContext.Schema);

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id");

        builder.Property(v => v.PatientId)
            .IsRequired()
            .HasColumnName("patient_id");

        builder.Property(v => v.EncounterId)
            .IsRequired(false)
            .HasColumnName("encounter_id");

        builder.Property(v => v.MeasurementType)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("measurement_type");

        builder.Property(v => v.Value)
            .HasPrecision(10, 2)
            .IsRequired()
            .HasColumnName("value");

        builder.Property(v => v.Unit)
            .HasMaxLength(32)
            .IsRequired()
            .HasColumnName("unit");

        builder.Property(v => v.Interpretation)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("interpretation");

        builder.Property(v => v.MeasurementMethod)
            .HasMaxLength(128)
            .IsRequired(false)
            .HasColumnName("measurement_method");

        builder.Property(v => v.MeasuredAtUtc)
            .IsRequired()
            .HasColumnName("measured_at_utc");

        builder.Property(v => v.Notes)
            .HasMaxLength(1000)
            .IsRequired(false)
            .HasColumnName("notes");

        builder.Property(v => v.RecordedByPractitionerId)
            .IsRequired()
            .HasColumnName("recorded_by_practitioner_id");

        builder.Property(v => v.RecordedAtUtc)
            .IsRequired()
            .HasColumnName("recorded_at_utc");

        builder.Property(v => v.UpdatedAtUtc)
            .IsRequired(false)
            .HasColumnName("updated_at_utc");

        builder.Property(v => v.IsEnteredInError)
            .IsRequired()
            .HasColumnName("is_entered_in_error");

        builder.Property(v => v.EnteredInErrorReason)
            .HasMaxLength(500)
            .IsRequired(false)
            .HasColumnName("entered_in_error_reason");

        builder.Property(v => v.Version)
            .IsConcurrencyToken()
            .IsRequired()
            .HasColumnName("version");

        builder.HasIndex(v => v.PatientId)
            .HasDatabaseName("ix_vital_sign_observations_patient_id");

        builder.HasIndex(v => v.EncounterId)
            .HasDatabaseName("ix_vital_sign_observations_encounter_id");

        builder.HasIndex(v => v.MeasuredAtUtc)
            .HasDatabaseName("ix_vital_sign_observations_measured_at_utc");
    }
}
