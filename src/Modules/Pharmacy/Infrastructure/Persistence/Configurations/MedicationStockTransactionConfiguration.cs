using HospitalManagement.Modules.Pharmacy.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence.Configurations;

public sealed class MedicationStockTransactionConfiguration : IEntityTypeConfiguration<MedicationStockTransaction>
{
    public void Configure(EntityTypeBuilder<MedicationStockTransaction> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("medication_stock_transactions", PharmacyDbContext.SchemaName);

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(t => t.StockItemId)
            .HasColumnName("stock_item_id")
            .IsRequired();

        builder.Property(t => t.TransactionType)
            .HasColumnName("transaction_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(t => t.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(t => t.PreviousQuantityOnHand)
            .HasColumnName("previous_quantity_on_hand")
            .IsRequired();

        builder.Property(t => t.NewQuantityOnHand)
            .HasColumnName("new_quantity_on_hand")
            .IsRequired();

        builder.Property(t => t.ReferenceId)
            .HasColumnName("reference_id")
            .HasMaxLength(64);

        builder.Property(t => t.Notes)
            .HasColumnName("notes")
            .HasMaxLength(512);

        builder.Property(t => t.PerformedByUserId)
            .HasColumnName("performed_by_user_id");

        builder.Property(t => t.PerformedAtUtc)
            .HasColumnName("performed_at_utc")
            .IsRequired();

        builder.HasIndex(t => t.StockItemId)
            .HasDatabaseName("ix_medication_stock_transactions_stock_item_id");

        builder.HasIndex(t => t.PerformedAtUtc)
            .HasDatabaseName("ix_medication_stock_transactions_performed_at");
    }
}
