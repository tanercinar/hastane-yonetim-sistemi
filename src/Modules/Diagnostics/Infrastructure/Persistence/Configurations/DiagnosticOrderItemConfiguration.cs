using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

public sealed class DiagnosticOrderItemConfiguration : IEntityTypeConfiguration<DiagnosticOrderItem>
{
    public void Configure(EntityTypeBuilder<DiagnosticOrderItem> builder)
    {
        builder.ToTable("diagnostic_order_items", "diagnostics");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id");

        builder.Property(i => i.DiagnosticOrderId)
            .HasColumnName("diagnostic_order_id")
            .IsRequired();

        builder.Property(i => i.CatalogCode)
            .HasColumnName("catalog_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.CatalogItemName)
            .HasColumnName("catalog_item_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Category)
            .HasColumnName("category")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(i => i.SpecialInstructions)
            .HasColumnName("special_instructions")
            .HasMaxLength(500);

        builder.Property(i => i.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(i => i.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(i => i.DiagnosticOrderId);
        builder.HasIndex(i => i.CatalogCode);
    }
}
