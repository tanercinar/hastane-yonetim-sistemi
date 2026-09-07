using HospitalManagement.Modules.Pharmacy.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence.Configurations;

public sealed class MedicationStockItemConfiguration : IEntityTypeConfiguration<MedicationStockItem>
{
    public void Configure(EntityTypeBuilder<MedicationStockItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "medication_stock_items",
            PharmacyDbContext.SchemaName,
            t =>
            {
                t.HasCheckConstraint("ck_medication_stock_items_quantity_on_hand", "quantity_on_hand >= 0");
                t.HasCheckConstraint("ck_medication_stock_items_quantity_reserved", "quantity_reserved >= 0");
                t.HasCheckConstraint("ck_medication_stock_items_reserved_not_above_on_hand", "quantity_reserved <= quantity_on_hand");
                t.HasCheckConstraint("ck_medication_stock_items_reorder_level", "reorder_level >= 0");
            });

        builder.HasKey(s => s.Id);

        builder.Ignore(s => s.QuantityAvailable);
        builder.Ignore(s => s.IsLowStock);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.DepartmentId)
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(s => s.Location)
            .HasColumnName("location")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(s => s.MedicationCatalogItemId)
            .HasColumnName("medication_catalog_item_id")
            .IsRequired();

        builder.Property(s => s.LotNumber)
            .HasColumnName("lot_number")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(s => s.ExpirationDateUtc)
            .HasColumnName("expiration_date_utc")
            .IsRequired();

        builder.Property(s => s.QuantityOnHand)
            .HasColumnName("quantity_on_hand")
            .IsRequired();

        builder.Property(s => s.QuantityReserved)
            .HasColumnName("quantity_reserved")
            .IsRequired();

        builder.Property(s => s.ReorderLevel)
            .HasColumnName("reorder_level")
            .IsRequired();

        builder.Property(s => s.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(s => s.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(s => new { s.DepartmentId, s.MedicationCatalogItemId, s.Location, s.LotNumber })
            .IsUnique()
            .HasDatabaseName("ux_medication_stock_items_catalog_location_lot");

        builder.HasIndex(s => s.DepartmentId)
            .HasDatabaseName("ix_medication_stock_items_department_id");

        builder.HasIndex(s => new { s.MedicationCatalogItemId, s.ExpirationDateUtc })
            .HasDatabaseName("ix_medication_stock_items_fefo");
    }
}
