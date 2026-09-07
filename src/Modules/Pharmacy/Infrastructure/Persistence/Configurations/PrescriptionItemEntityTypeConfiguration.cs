using HospitalManagement.Modules.Pharmacy.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence.Configurations;

public sealed class PrescriptionItemEntityTypeConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("prescription_items", PharmacyDbContext.SchemaName);

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(i => i.PrescriptionId)
            .HasColumnName("prescription_id")
            .IsRequired();

        builder.Property(i => i.MedicationCatalogItemId)
            .HasColumnName("medication_catalog_item_id")
            .IsRequired();

        builder.Property(i => i.MedicationCode)
            .HasColumnName("medication_code")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(i => i.BrandName)
            .HasColumnName("brand_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(i => i.GenericName)
            .HasColumnName("generic_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(i => i.Form)
            .HasColumnName("form")
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(i => i.Route)
            .HasColumnName("route")
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(i => i.Dose)
            .HasColumnName("dose")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(i => i.DoseUnit)
            .HasColumnName("dose_unit")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(i => i.Frequency)
            .HasColumnName("frequency")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(i => i.DurationDays)
            .HasColumnName("duration_days")
            .IsRequired();

        builder.Property(i => i.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(i => i.QuantityUnit)
            .HasColumnName("quantity_unit")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(i => i.DispensedQuantity)
            .HasColumnName("dispensed_quantity")
            .IsRequired();

        builder.Property(i => i.Instructions)
            .HasColumnName("instructions")
            .HasMaxLength(512);

        builder.Property(i => i.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(i => i.PrescriptionId)
            .HasDatabaseName("ix_prescription_items_prescription_id");

        builder.HasIndex(i => i.MedicationCatalogItemId)
            .HasDatabaseName("ix_prescription_items_medication_catalog_item_id");
    }
}
