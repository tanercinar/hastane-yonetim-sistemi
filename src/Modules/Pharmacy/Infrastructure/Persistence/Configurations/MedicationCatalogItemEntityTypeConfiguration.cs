using HospitalManagement.Modules.Pharmacy.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence.Configurations;

public sealed class MedicationCatalogItemEntityTypeConfiguration : IEntityTypeConfiguration<MedicationCatalogItem>
{
    public void Configure(EntityTypeBuilder<MedicationCatalogItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("medication_catalog_items", PharmacyDbContext.SchemaName);

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(m => m.Code)
            .HasColumnName("code")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(m => m.BrandName)
            .HasColumnName("brand_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(m => m.GenericName)
            .HasColumnName("generic_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(m => m.Form)
            .HasColumnName("form")
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(m => m.StrengthValue)
            .HasColumnName("strength_value")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(m => m.StrengthUnit)
            .HasColumnName("strength_unit")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(m => m.Route)
            .HasColumnName("route")
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(m => m.AtcCode)
            .HasColumnName("atc_code")
            .HasMaxLength(16);

        builder.Property(m => m.Description)
            .HasColumnName("description")
            .HasMaxLength(1024);

        builder.Property(m => m.CatalogVersion)
            .HasColumnName("catalog_version")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(m => m.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(m => m.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(m => m.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(m => new { m.Code, m.CatalogVersion })
            .IsUnique()
            .HasDatabaseName("ux_medication_catalog_items_code_version");

        builder.HasIndex(m => m.GenericName)
            .HasDatabaseName("ix_medication_catalog_items_generic_name");

        builder.HasIndex(m => m.BrandName)
            .HasDatabaseName("ix_medication_catalog_items_brand_name");

        builder.HasIndex(m => m.AtcCode)
            .HasDatabaseName("ix_medication_catalog_items_atc_code");

        builder.HasIndex(m => m.CatalogVersion)
            .HasDatabaseName("ix_medication_catalog_items_catalog_version");
    }
}
