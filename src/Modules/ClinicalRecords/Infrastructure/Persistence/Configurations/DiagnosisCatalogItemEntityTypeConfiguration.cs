using HospitalManagement.Modules.ClinicalRecords.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Configurations;

public sealed class DiagnosisCatalogItemEntityTypeConfiguration : IEntityTypeConfiguration<DiagnosisCatalogItem>
{
    public void Configure(EntityTypeBuilder<DiagnosisCatalogItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("diagnosis_catalog_items", ClinicalRecordsDbContext.Schema);

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.Code)
            .HasMaxLength(32)
            .IsRequired()
            .HasColumnName("code");

        builder.Property(c => c.NameTurkish)
            .HasMaxLength(500)
            .IsRequired()
            .HasColumnName("name_turkish");

        builder.Property(c => c.NameEnglish)
            .HasMaxLength(500)
            .IsRequired()
            .HasColumnName("name_english");

        builder.Property(c => c.Chapter)
            .HasMaxLength(256)
            .IsRequired()
            .HasColumnName("chapter");

        builder.Property(c => c.Block)
            .HasMaxLength(256)
            .IsRequired()
            .HasColumnName("block");

        builder.Property(c => c.CatalogVersion)
            .HasMaxLength(64)
            .IsRequired()
            .HasColumnName("catalog_version");

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasColumnName("is_active");

        builder.HasIndex(c => c.Code)
            .IsUnique()
            .HasDatabaseName("ux_diagnosis_catalog_items_code");

        builder.HasIndex(c => c.NameTurkish)
            .HasDatabaseName("ix_diagnosis_catalog_items_name_turkish");
    }
}
