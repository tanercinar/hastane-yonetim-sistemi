using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

public sealed class CriticalResultNotificationConfiguration : IEntityTypeConfiguration<CriticalResultNotification>
{
    public void Configure(EntityTypeBuilder<CriticalResultNotification> builder)
    {
        builder.ToTable("critical_result_notifications", "diagnostics");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(n => n.LabResultId)
            .HasColumnName("lab_result_id")
            .IsRequired();

        builder.Property(n => n.DiagnosticOrderId)
            .HasColumnName("diagnostic_order_id")
            .IsRequired();

        builder.Property(n => n.DiagnosticOrderItemId)
            .HasColumnName("diagnostic_order_item_id")
            .IsRequired();

        builder.Property(n => n.PatientId)
            .HasColumnName("patient_id")
            .IsRequired();

        builder.Property(n => n.ParameterCode)
            .HasColumnName("parameter_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.ParameterName)
            .HasColumnName("parameter_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(n => n.NumericValue)
            .HasColumnName("numeric_value")
            .HasPrecision(18, 4);

        builder.Property(n => n.StringValue)
            .HasColumnName("string_value")
            .HasMaxLength(200);

        builder.Property(n => n.Unit)
            .HasColumnName("unit")
            .HasMaxLength(50);

        builder.Property(n => n.Flag)
            .HasColumnName("flag")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(n => n.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(n => n.EscalationLevel)
            .HasColumnName("escalation_level")
            .IsRequired();

        builder.Property(n => n.ResponsibleDoctorUserId)
            .HasColumnName("responsible_doctor_user_id");

        builder.Property(n => n.AcknowledgedByUserId)
            .HasColumnName("acknowledged_by_user_id");

        builder.Property(n => n.AcknowledgedAtUtc)
            .HasColumnName("acknowledged_at_utc");

        builder.Property(n => n.AcknowledgmentNotes)
            .HasColumnName("acknowledgment_notes")
            .HasMaxLength(1000);

        builder.Property(n => n.EscalatedAtUtc)
            .HasColumnName("escalated_at_utc");

        builder.Property(n => n.EscalationReason)
            .HasColumnName("escalation_reason")
            .HasMaxLength(500);

        builder.Property(n => n.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(n => n.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(n => n.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        builder.HasIndex(n => n.PatientId);
        builder.HasIndex(n => n.DiagnosticOrderId);
        builder.HasIndex(n => n.LabResultId);
        builder.HasIndex(n => new { n.LabResultId, n.ParameterCode }).IsUnique();
        builder.HasIndex(n => n.Status);
    }
}
