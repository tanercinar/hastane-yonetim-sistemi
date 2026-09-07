using HospitalManagement.Modules.ClinicalRecords.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Configurations;

public sealed class EncounterDiagnosisEntityTypeConfiguration : IEntityTypeConfiguration<EncounterDiagnosis>
{
    public void Configure(EntityTypeBuilder<EncounterDiagnosis> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("encounter_diagnoses", ClinicalRecordsDbContext.Schema);

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id");

        builder.Property(d => d.EncounterId)
            .IsRequired()
            .HasColumnName("encounter_id");

        builder.Property(d => d.PatientId)
            .IsRequired()
            .HasColumnName("patient_id");

        builder.Property(d => d.DiagnosedByPractitionerId)
            .IsRequired()
            .HasColumnName("diagnosed_by_practitioner_id");

        builder.Property(d => d.DiagnosisType)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("diagnosis_type");

        builder.Property(d => d.IsCoded)
            .IsRequired()
            .HasColumnName("is_coded");

        builder.Property(d => d.Icd10Code)
            .HasMaxLength(32)
            .IsRequired(false)
            .HasColumnName("icd10_code");

        builder.Property(d => d.DiagnosisTitle)
            .HasMaxLength(500)
            .IsRequired()
            .HasColumnName("diagnosis_title");

        builder.Property(d => d.CatalogVersion)
            .HasMaxLength(64)
            .IsRequired(false)
            .HasColumnName("catalog_version");

        builder.Property(d => d.Notes)
            .HasMaxLength(1000)
            .IsRequired(false)
            .HasColumnName("notes");

        builder.Property(d => d.DiagnosedAtUtc)
            .IsRequired()
            .HasColumnName("diagnosed_at_utc");

        builder.Property(d => d.UpdatedAtUtc)
            .IsRequired(false)
            .HasColumnName("updated_at_utc");

        builder.Property(d => d.IsEnteredInError)
            .IsRequired()
            .HasColumnName("is_entered_in_error");

        builder.Property(d => d.EnteredInErrorReason)
            .HasMaxLength(500)
            .IsRequired(false)
            .HasColumnName("entered_in_error_reason");

        builder.Property(d => d.Version)
            .IsConcurrencyToken()
            .IsRequired()
            .HasColumnName("version");

        builder.HasIndex(d => d.EncounterId)
            .HasDatabaseName("ix_encounter_diagnoses_encounter_id");

        builder.HasIndex(d => d.PatientId)
            .HasDatabaseName("ix_encounter_diagnoses_patient_id");

        builder.HasIndex(d => d.Icd10Code)
            .HasDatabaseName("ix_encounter_diagnoses_icd10_code");
    }
}
