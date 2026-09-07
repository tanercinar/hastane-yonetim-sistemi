using HospitalManagement.Modules.ClinicalRecords.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Configurations;

public sealed class AllergyIntoleranceEntityTypeConfiguration : IEntityTypeConfiguration<AllergyIntolerance>
{
    public void Configure(EntityTypeBuilder<AllergyIntolerance> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("allergy_intolerances", ClinicalRecordsDbContext.Schema);

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.PatientId)
            .IsRequired()
            .HasColumnName("patient_id");

        builder.Property(a => a.EncounterId)
            .IsRequired(false)
            .HasColumnName("encounter_id");

        builder.Property(a => a.Substance)
            .HasMaxLength(256)
            .IsRequired()
            .HasColumnName("substance");

        builder.Property(a => a.Category)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("category");

        builder.Property(a => a.Criticality)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("criticality");

        builder.Property(a => a.ClinicalStatus)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("clinical_status");

        builder.Property(a => a.VerificationStatus)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("verification_status");

        builder.Property(a => a.Manifestation)
            .HasMaxLength(500)
            .IsRequired(false)
            .HasColumnName("manifestation");

        builder.Property(a => a.OnsetDateTimeUtc)
            .IsRequired(false)
            .HasColumnName("onset_date_time_utc");

        builder.Property(a => a.Notes)
            .HasMaxLength(1000)
            .IsRequired(false)
            .HasColumnName("notes");

        builder.Property(a => a.RecordedByPractitionerId)
            .IsRequired()
            .HasColumnName("recorded_by_practitioner_id");

        builder.Property(a => a.RecordedAtUtc)
            .IsRequired()
            .HasColumnName("recorded_at_utc");

        builder.Property(a => a.UpdatedAtUtc)
            .IsRequired(false)
            .HasColumnName("updated_at_utc");

        builder.Property(a => a.EnteredInErrorReason)
            .HasMaxLength(500)
            .IsRequired(false)
            .HasColumnName("entered_in_error_reason");

        builder.Property(a => a.Version)
            .IsConcurrencyToken()
            .IsRequired()
            .HasColumnName("version");

        builder.HasIndex(a => a.PatientId)
            .HasDatabaseName("ix_allergy_intolerances_patient_id");

        builder.HasIndex(a => a.ClinicalStatus)
            .HasDatabaseName("ix_allergy_intolerances_clinical_status");
    }
}
