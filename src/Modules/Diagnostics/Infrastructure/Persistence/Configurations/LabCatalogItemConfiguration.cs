using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

public sealed class LabCatalogItemConfiguration : IEntityTypeConfiguration<LabCatalogItem>
{
    public void Configure(EntityTypeBuilder<LabCatalogItem> builder)
    {
        builder.ToTable("lab_catalog_items", "diagnostics");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(c => c.Code)
            .IsUnique();

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Category)
            .HasColumnName("category")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.SpecimenType)
            .HasColumnName("specimen_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.ContainerType)
            .HasColumnName("container_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.IsPanel)
            .HasColumnName("is_panel")
            .IsRequired();

        builder.Property(c => c.TurnaroundMinutes)
            .HasColumnName("turnaround_minutes")
            .IsRequired();

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(c => c.CatalogVersion)
            .HasColumnName("catalog_version")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(c => c.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasMany(c => c.Parameters)
            .WithOne()
            .HasForeignKey(p => p.LabCatalogItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.Category);
        builder.HasIndex(c => c.IsActive);
    }
}
