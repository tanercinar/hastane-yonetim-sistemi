using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

public sealed class LabResultConfiguration : IEntityTypeConfiguration<LabResult>
{
    public void Configure(EntityTypeBuilder<LabResult> builder)
    {
        builder.ToTable("lab_results", "diagnostics");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.DiagnosticOrderId)
            .HasColumnName("diagnostic_order_id")
            .IsRequired();

        builder.Property(r => r.DiagnosticOrderItemId)
            .HasColumnName("diagnostic_order_item_id")
            .IsRequired();

        builder.Property(r => r.SpecimenId)
            .HasColumnName("specimen_id");

        builder.Property(r => r.PatientId)
            .HasColumnName("patient_id")
            .IsRequired();

        builder.Property(r => r.CatalogCode)
            .HasColumnName("catalog_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.CatalogItemName)
            .HasColumnName("catalog_item_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.TechnicallyApprovedByUserId)
            .HasColumnName("technically_approved_by_user_id");

        builder.Property(r => r.TechnicallyApprovedAtUtc)
            .HasColumnName("technically_approved_at_utc");

        builder.Property(r => r.ClinicallyApprovedByUserId)
            .HasColumnName("clinically_approved_by_user_id");

        builder.Property(r => r.ClinicallyApprovedAtUtc)
            .HasColumnName("clinically_approved_at_utc");

        builder.Property(r => r.PreviousResultId)
            .HasColumnName("previous_result_id");

        builder.Property(r => r.CorrectionReason)
            .HasColumnName("correction_reason")
            .HasMaxLength(500);

        builder.Property(r => r.ClinicalNotes)
            .HasColumnName("clinical_notes")
            .HasMaxLength(2000);

        builder.Property(r => r.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(r => r.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(i => i.LabResultId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(r => r.DiagnosticOrderId);
        builder.HasIndex(r => r.DiagnosticOrderItemId)
            .HasDatabaseName("UX_lab_results_order_item_root")
            .IsUnique()
            .HasFilter("previous_result_id IS NULL");
        builder.HasIndex(r => r.PatientId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.CreatedAtUtc);
    }
}
