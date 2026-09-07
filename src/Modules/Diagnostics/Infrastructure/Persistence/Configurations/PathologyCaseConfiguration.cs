using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

public sealed class PathologyCaseConfiguration : IEntityTypeConfiguration<PathologyCase>
{
    public void Configure(EntityTypeBuilder<PathologyCase> builder)
    {
        builder.ToTable("pathology_cases", "diagnostics");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.DiagnosticOrderId)
            .HasColumnName("diagnostic_order_id")
            .IsRequired();

        builder.Property(c => c.DiagnosticOrderItemId)
            .HasColumnName("diagnostic_order_item_id")
            .IsRequired();

        builder.Property(c => c.PatientId)
            .HasColumnName("patient_id")
            .IsRequired();

        builder.Property(c => c.PathologyNumber)
            .HasColumnName("pathology_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.SpecimenType)
            .HasColumnName("specimen_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.AnatomicSite)
            .HasColumnName("anatomic_site")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.ClinicalHistoryAndDiagnosis)
            .HasColumnName("clinical_history_and_diagnosis")
            .HasMaxLength(1000);

        builder.Property(c => c.FixativeUsed)
            .HasColumnName("fixative_used")
            .HasMaxLength(100);

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.ReceivedAtUtc)
            .HasColumnName("received_at_utc");

        builder.Property(c => c.ReceivedByUserId)
            .HasColumnName("received_by_user_id");

        builder.Property(c => c.GrossDescription)
            .HasColumnName("gross_description")
            .HasMaxLength(4000);

        builder.Property(c => c.GrossExamAtUtc)
            .HasColumnName("gross_exam_at_utc");

        builder.Property(c => c.GrossExamByUserId)
            .HasColumnName("gross_exam_by_user_id");

        builder.Property(c => c.MicroscopicDescription)
            .HasColumnName("microscopic_description")
            .HasMaxLength(4000);

        builder.Property(c => c.MicroscopicExamAtUtc)
            .HasColumnName("microscopic_exam_at_utc");

        builder.Property(c => c.MicroscopicExamByUserId)
            .HasColumnName("microscopic_exam_by_user_id");

        builder.Property(c => c.PathologicalDiagnosis)
            .HasColumnName("pathological_diagnosis")
            .HasMaxLength(4000);

        builder.Property(c => c.ReportDraftedAtUtc)
            .HasColumnName("report_drafted_at_utc");

        builder.Property(c => c.ReportFinalizedAtUtc)
            .HasColumnName("report_finalized_at_utc");

        builder.Property(c => c.PathologistUserId)
            .HasColumnName("pathologist_user_id");

        builder.Property(c => c.CorrectionReason)
            .HasColumnName("correction_reason")
            .HasMaxLength(500);

        builder.Property(c => c.PreviousCaseId)
            .HasColumnName("previous_case_id");

        builder.Property(c => c.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasMaxLength(500);

        builder.Property(c => c.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(c => c.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(c => c.PathologyNumber)
            .IsUnique();

        builder.HasIndex(c => c.DiagnosticOrderId);
        builder.HasIndex(c => c.DiagnosticOrderItemId);
        builder.HasIndex(c => c.PatientId);
        builder.HasIndex(c => c.Status);
    }
}
