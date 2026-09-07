using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

public sealed class BloodUnitConfiguration : IEntityTypeConfiguration<BloodUnit>
{
    public void Configure(EntityTypeBuilder<BloodUnit> builder)
    {
        builder.ToTable("blood_units", "diagnostics");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id");

        builder.Property(u => u.UnitNumber)
            .HasColumnName("unit_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.ProductType)
            .HasColumnName("product_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(u => u.BloodGroup)
            .HasColumnName("blood_group")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.VolumeMl)
            .HasColumnName("volume_ml")
            .IsRequired();

        builder.Property(u => u.DonationDateUtc)
            .HasColumnName("donation_date_utc")
            .IsRequired();

        builder.Property(u => u.ExpiryDateUtc)
            .HasColumnName("expiry_date_utc")
            .IsRequired();

        builder.Property(u => u.StorageLocation)
            .HasColumnName("storage_location")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(u => u.ReservedForPatientId)
            .HasColumnName("reserved_for_patient_id");

        builder.Property(u => u.ReservedUntilUtc)
            .HasColumnName("reserved_until_utc");

        builder.Property(u => u.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(u => u.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(u => u.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(u => u.UnitNumber)
            .IsUnique();

        builder.HasIndex(u => u.ProductType);
        builder.HasIndex(u => u.BloodGroup);
        builder.HasIndex(u => u.Status);
        builder.HasIndex(u => u.ReservedForPatientId);
    }
}
