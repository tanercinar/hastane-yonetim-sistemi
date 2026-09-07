using HospitalManagement.Modules.Inpatient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Configurations;

public sealed class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder.ToTable("wards", "inpatient");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(w => w.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(w => w.Building)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(w => w.Floor)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(w => w.WardType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(w => w.Code)
            .IsUnique();

        builder.HasIndex(w => w.DepartmentId);

        builder.HasMany(w => w.Rooms)
            .WithOne()
            .HasForeignKey(r => r.WardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
