using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

public sealed class LabCatalogParameterConfiguration : IEntityTypeConfiguration<LabCatalogParameter>
{
    public void Configure(EntityTypeBuilder<LabCatalogParameter> builder)
    {
        builder.ToTable("lab_catalog_parameters", "diagnostics");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.LabCatalogItemId)
            .HasColumnName("lab_catalog_item_id")
            .IsRequired();

        builder.Property(p => p.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Unit)
            .HasColumnName("unit")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.ReferenceRangeLow)
            .HasColumnName("reference_range_low")
            .HasPrecision(18, 4);

        builder.Property(p => p.ReferenceRangeHigh)
            .HasColumnName("reference_range_high")
            .HasPrecision(18, 4);

        builder.Property(p => p.CriticalLow)
            .HasColumnName("critical_low")
            .HasPrecision(18, 4);

        builder.Property(p => p.CriticalHigh)
            .HasColumnName("critical_high")
            .HasPrecision(18, 4);

        builder.Property(p => p.ValueType)
            .HasColumnName("value_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.HasIndex(p => p.LabCatalogItemId);
        builder.HasIndex(p => p.Code);
    }
}
