using HospitalManagement.Modules.ClinicalRecords.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Configurations;

public sealed class ClinicalProblemEntityTypeConfiguration : IEntityTypeConfiguration<ClinicalProblem>
{
    public void Configure(EntityTypeBuilder<ClinicalProblem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("clinical_problems", ClinicalRecordsDbContext.Schema);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.PatientId)
            .IsRequired()
            .HasColumnName("patient_id");

        builder.Property(p => p.EncounterId)
            .IsRequired(false)
            .HasColumnName("encounter_id");

        builder.Property(p => p.ProblemTitle)
            .HasMaxLength(256)
            .IsRequired()
            .HasColumnName("problem_title");

        builder.Property(p => p.Code)
            .HasMaxLength(64)
            .IsRequired(false)
            .HasColumnName("code");

        builder.Property(p => p.Category)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("category");

        builder.Property(p => p.ClinicalStatus)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("clinical_status");

        builder.Property(p => p.VerificationStatus)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("verification_status");

        builder.Property(p => p.OnsetDate)
            .IsRequired(false)
            .HasColumnName("onset_date");

        builder.Property(p => p.ResolvedDate)
            .IsRequired(false)
            .HasColumnName("resolved_date");

        builder.Property(p => p.Notes)
            .HasMaxLength(1000)
            .IsRequired(false)
            .HasColumnName("notes");

        builder.Property(p => p.RecordedByPractitionerId)
            .IsRequired()
            .HasColumnName("recorded_by_practitioner_id");

        builder.Property(p => p.RecordedAtUtc)
            .IsRequired()
            .HasColumnName("recorded_at_utc");

        builder.Property(p => p.UpdatedAtUtc)
            .IsRequired(false)
            .HasColumnName("updated_at_utc");

        builder.Property(p => p.EnteredInErrorReason)
            .HasMaxLength(500)
            .IsRequired(false)
            .HasColumnName("entered_in_error_reason");

        builder.Property(p => p.Version)
            .IsConcurrencyToken()
            .IsRequired()
            .HasColumnName("version");

        builder.HasIndex(p => p.PatientId)
            .HasDatabaseName("ix_clinical_problems_patient_id");

        builder.HasIndex(p => p.Category)
            .HasDatabaseName("ix_clinical_problems_category");

        builder.HasIndex(p => p.ClinicalStatus)
            .HasDatabaseName("ix_clinical_problems_clinical_status");
    }
}
