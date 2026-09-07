using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence.Configurations;

public sealed class IcuBedConfiguration : IEntityTypeConfiguration<IcuBed>
{
    public void Configure(EntityTypeBuilder<IcuBed> builder)
    {
        builder.ToTable("IcuBeds", "surgery");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BedCode)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(b => b.BedCode)
            .IsUnique();

        builder.Property(b => b.BedName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(b => b.UnitName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(b => b.IsActive)
            .IsRequired();

        builder.Property(b => b.Version)
            .IsRowVersion();
    }
}
