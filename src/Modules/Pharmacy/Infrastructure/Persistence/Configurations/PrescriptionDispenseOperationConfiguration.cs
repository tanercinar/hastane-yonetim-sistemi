using HospitalManagement.Modules.Pharmacy.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence.Configurations;

public sealed class PrescriptionDispenseOperationConfiguration
    : IEntityTypeConfiguration<PrescriptionDispenseOperation>
{
    public void Configure(EntityTypeBuilder<PrescriptionDispenseOperation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("prescription_dispense_operations", PharmacyDbContext.SchemaName);
        builder.HasKey(operation => operation.Id);

        builder.Property(operation => operation.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(operation => operation.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .IsRequired();
        builder.Property(operation => operation.PrescriptionId)
            .HasColumnName("prescription_id")
            .IsRequired();
        builder.Property(operation => operation.RequestFingerprint)
            .HasColumnName("request_fingerprint")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(operation => operation.CompletedAtUtc)
            .HasColumnName("completed_at_utc")
            .IsRequired();

        builder.HasIndex(operation => operation.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_prescription_dispense_operations_idempotency_key");
        builder.HasIndex(operation => operation.PrescriptionId)
            .HasDatabaseName("ix_prescription_dispense_operations_prescription_id");
    }
}
