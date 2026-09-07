using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

public sealed class DiagnosticOrderConfiguration : IEntityTypeConfiguration<DiagnosticOrder>
{
    public void Configure(EntityTypeBuilder<DiagnosticOrder> builder)
    {
        builder.ToTable("diagnostic_orders", "diagnostics");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id");

        builder.Property(o => o.OrderNumber)
            .HasColumnName("order_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.Property(o => o.PatientId)
            .HasColumnName("patient_id")
            .IsRequired();

        builder.Property(o => o.EncounterId)
            .HasColumnName("encounter_id")
            .IsRequired();

        builder.Property(o => o.PlacingDoctorId)
            .HasColumnName("placing_doctor_id")
            .IsRequired();

        builder.Property(o => o.DepartmentId)
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(o => o.OrderType)
            .HasColumnName("order_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.ClinicalIndication)
            .HasColumnName("clinical_indication")
            .HasMaxLength(500);

        builder.Property(o => o.OrderNotes)
            .HasColumnName("order_notes")
            .HasMaxLength(1000);

        builder.Property(o => o.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasMaxLength(500);

        builder.Property(o => o.EnteredInErrorReason)
            .HasColumnName("entered_in_error_reason")
            .HasMaxLength(500);

        builder.Property(o => o.PlacedAtUtc)
            .HasColumnName("placed_at_utc");

        builder.Property(o => o.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        builder.Property(o => o.CancelledAtUtc)
            .HasColumnName("cancelled_at_utc");

        builder.Property(o => o.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(o => o.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(o => o.Version)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.DiagnosticOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(o => o.PatientId);
        builder.HasIndex(o => o.EncounterId);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.OrderType);
        builder.HasIndex(o => o.CreatedAtUtc);
    }
}
