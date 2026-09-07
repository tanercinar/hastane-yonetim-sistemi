using HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Configurations;

public sealed class DentalToothConditionConfiguration : IEntityTypeConfiguration<DentalToothCondition>
{
    public void Configure(EntityTypeBuilder<DentalToothCondition> builder)
    {
        builder.ToTable("DentalToothConditions", "specialty");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.PatientId)
            .IsRequired();

        builder.Property(t => t.ToothNumber)
            .IsRequired();

        builder.Property(t => t.Condition)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(t => t.AffectedSurfaces)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.Notes)
            .HasMaxLength(1000);

        builder.Property(t => t.RecordedByStaffId)
            .IsRequired();

        builder.Property(t => t.Version)
            .IsRequired();

        builder.Property(t => t.RecordedAtUtc)
            .IsRequired();

        builder.HasIndex(t => new { t.PatientId, t.ToothNumber });
        builder.HasIndex(t => new { t.PatientId, t.ToothNumber, t.Version })
            .IsUnique();
        builder.HasIndex(t => t.RecordedAtUtc);
    }
}
