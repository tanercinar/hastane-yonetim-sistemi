using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

public sealed class RadiologyStudyConfiguration : IEntityTypeConfiguration<RadiologyStudy>
{
    public void Configure(EntityTypeBuilder<RadiologyStudy> builder)
    {
        builder.ToTable("radiology_studies", "diagnostics");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.DiagnosticOrderId)
            .HasColumnName("diagnostic_order_id")
            .IsRequired();

        builder.Property(s => s.DiagnosticOrderItemId)
            .HasColumnName("diagnostic_order_item_id")
            .IsRequired();

        builder.Property(s => s.PatientId)
            .HasColumnName("patient_id")
            .IsRequired();

        builder.Property(s => s.AccessionNumber)
            .HasColumnName("accession_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.Modality)
            .HasColumnName("modality")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.ProcedureCode)
            .HasColumnName("procedure_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.ProcedureName)
            .HasColumnName("procedure_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.BodySite)
            .HasColumnName("body_site")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.ScheduledAtUtc)
            .HasColumnName("scheduled_at_utc");

        builder.Property(s => s.PerformedAtUtc)
            .HasColumnName("performed_at_utc");

        builder.Property(s => s.TechnicianUserId)
            .HasColumnName("technician_user_id");

        builder.Property(s => s.TechnicianNotes)
            .HasColumnName("technician_notes")
            .HasMaxLength(1000);

        builder.Property(s => s.RadiologistUserId)
            .HasColumnName("radiologist_user_id");

        builder.Property(s => s.ReportText)
            .HasColumnName("report_text")
            .HasMaxLength(4000);

        builder.Property(s => s.Impression)
            .HasColumnName("impression")
            .HasMaxLength(2000);

        builder.Property(s => s.ReportDraftedAtUtc)
            .HasColumnName("report_drafted_at_utc");

        builder.Property(s => s.ReportFinalizedAtUtc)
            .HasColumnName("report_finalized_at_utc");

        builder.Property(s => s.AddendumText)
            .HasColumnName("addendum_text")
            .HasMaxLength(4000);

        builder.Property(s => s.AddendumAddedAtUtc)
            .HasColumnName("addendum_added_at_utc");

        builder.Property(s => s.AddendumByUserId)
            .HasColumnName("addendum_by_user_id");

        builder.Property(s => s.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasMaxLength(500);

        builder.Property(s => s.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(s => s.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        builder.HasIndex(s => s.AccessionNumber).IsUnique();
        builder.HasIndex(s => s.PatientId);
        builder.HasIndex(s => s.DiagnosticOrderId);
        builder.HasIndex(s => s.DiagnosticOrderItemId).IsUnique();
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.Modality);
    }
}
