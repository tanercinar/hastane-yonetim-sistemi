using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

public sealed class CrossmatchRequestConfiguration : IEntityTypeConfiguration<CrossmatchRequest>
{
    public void Configure(EntityTypeBuilder<CrossmatchRequest> builder)
    {
        builder.ToTable("crossmatch_requests", "diagnostics");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.DiagnosticOrderId)
            .HasColumnName("diagnostic_order_id")
            .IsRequired();

        builder.Property(r => r.DiagnosticOrderItemId)
            .HasColumnName("diagnostic_order_item_id")
            .IsRequired();

        builder.Property(r => r.PatientId)
            .HasColumnName("patient_id")
            .IsRequired();

        builder.Property(r => r.PatientBloodGroup)
            .HasColumnName("patient_blood_group")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.RequestedProductType)
            .HasColumnName("requested_product_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.UnitsRequested)
            .HasColumnName("units_requested")
            .IsRequired();

        builder.Property(r => r.RequiredByUtc)
            .HasColumnName("required_by_utc");

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.CompatibilityResult)
            .HasColumnName("compatibility_result")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.TechnicianNotes)
            .HasColumnName("technician_notes")
            .HasMaxLength(1000);

        builder.Property(r => r.TestedAtUtc)
            .HasColumnName("tested_at_utc");

        builder.Property(r => r.TestedByUserId)
            .HasColumnName("tested_by_user_id");

        builder.Property(r => r.AllocatedBloodUnitId)
            .HasColumnName("allocated_blood_unit_id");

        builder.Property(r => r.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasMaxLength(500);

        builder.Property(r => r.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(r => r.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(r => r.DiagnosticOrderId);
        builder.HasIndex(r => r.DiagnosticOrderItemId);
        builder.HasIndex(r => r.PatientId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.CompatibilityResult);
    }
}
