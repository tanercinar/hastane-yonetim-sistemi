using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

public sealed class SpecimenConfiguration : IEntityTypeConfiguration<Specimen>
{
    public void Configure(EntityTypeBuilder<Specimen> builder)
    {
        builder.ToTable("specimens", "diagnostics");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.Barcode)
            .HasColumnName("barcode")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(s => s.Barcode)
            .IsUnique();

        builder.Property(s => s.DiagnosticOrderId)
            .HasColumnName("diagnostic_order_id")
            .IsRequired();

        builder.Property(s => s.PatientId)
            .HasColumnName("patient_id")
            .IsRequired();

        builder.Property(s => s.SpecimenType)
            .HasColumnName("specimen_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.ContainerType)
            .HasColumnName("container_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.CollectionNotes)
            .HasColumnName("collection_notes")
            .HasMaxLength(500);

        builder.Property(s => s.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(500);

        builder.Property(s => s.CollectedAtUtc)
            .HasColumnName("collected_at_utc");

        builder.Property(s => s.CollectedByUserId)
            .HasColumnName("collected_by_user_id");

        builder.Property(s => s.ReceivedAtUtc)
            .HasColumnName("received_at_utc");

        builder.Property(s => s.ReceivedByUserId)
            .HasColumnName("received_by_user_id");

        builder.Property(s => s.RejectedAtUtc)
            .HasColumnName("rejected_at_utc");

        builder.Property(s => s.RejectedByUserId)
            .HasColumnName("rejected_by_user_id");

        builder.Property(s => s.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(s => s.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasMany(s => s.Transitions)
            .WithOne()
            .HasForeignKey(t => t.SpecimenId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Transitions)
            .HasField("_transitions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(s => s.DiagnosticOrderId);
        builder.HasIndex(s => s.PatientId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.CreatedAtUtc);
    }
}
