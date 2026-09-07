using HospitalManagement.Modules.Inpatient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Configurations;

public sealed class InpatientTransferConfiguration : IEntityTypeConfiguration<InpatientTransfer>
{
    public void Configure(EntityTypeBuilder<InpatientTransfer> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("transfers", "inpatient");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.AdmissionId)
            .IsRequired();

        builder.Property(t => t.PatientId)
            .IsRequired();

        builder.Property(t => t.SourceWardId)
            .IsRequired();

        builder.Property(t => t.SourceBedId)
            .IsRequired();

        builder.Property(t => t.TargetWardId)
            .IsRequired();

        builder.Property(t => t.TargetBedId);

        builder.Property(t => t.TransferReason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.ClinicalNotes)
            .HasMaxLength(1000);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(t => t.CancellationReason)
            .HasMaxLength(500);

        builder.Property(t => t.Version)
            .IsConcurrencyToken();

        builder.HasIndex(t => t.AdmissionId)
            .HasDatabaseName("IX_transfers_admission_id_active")
            .IsUnique()
            .HasFilter("\"Status\" IN ('Requested', 'Accepted')");
        builder.HasIndex(t => t.PatientId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.RequestedAtUtc);
    }
}
