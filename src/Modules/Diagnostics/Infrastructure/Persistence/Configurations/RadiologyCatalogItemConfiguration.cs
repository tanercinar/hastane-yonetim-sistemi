using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

public sealed class RadiologyCatalogItemConfiguration : IEntityTypeConfiguration<RadiologyCatalogItem>
{
    public void Configure(EntityTypeBuilder<RadiologyCatalogItem> builder)
    {
        builder.ToTable("radiology_catalog_items", "diagnostics");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Modality)
            .HasColumnName("modality")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.BodySite)
            .HasColumnName("body_site")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(c => c.PreparationInstructions)
            .HasColumnName("preparation_instructions")
            .HasMaxLength(500);

        builder.Property(c => c.ContrastRequired)
            .HasColumnName("contrast_required")
            .IsRequired();

        builder.Property(c => c.EstimatedDurationMinutes)
            .HasColumnName("estimated_duration_minutes")
            .IsRequired();

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.Modality);
        builder.HasIndex(c => c.IsActive);
    }
}
