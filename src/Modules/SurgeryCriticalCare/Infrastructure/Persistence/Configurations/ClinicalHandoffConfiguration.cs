using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence.Configurations;

public sealed class ClinicalHandoffConfiguration : IEntityTypeConfiguration<ClinicalHandoff>
{
    public void Configure(EntityTypeBuilder<ClinicalHandoff> builder)
    {
        builder.ToTable("ClinicalHandoffs", "surgery");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.HandoffProtocolNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(h => h.HandoffProtocolNumber)
            .IsUnique();

        builder.Property(h => h.PatientId)
            .IsRequired();

        builder.Property(h => h.SourceArea)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(h => h.SourceLocationDetails)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(h => h.DestinationArea)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(h => h.DestinationLocationDetails)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(h => h.HandingOverStaffId)
            .IsRequired();

        builder.Property(h => h.Situation)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(h => h.Background)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(h => h.Assessment)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(h => h.Recommendation)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(h => h.CriticalAlerts)
            .HasMaxLength(1000);

        builder.Property(h => h.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(h => h.StatusReason)
            .HasMaxLength(1000);

        builder.Property(h => h.HandedOverAtUtc)
            .IsRequired();

        builder.Property(h => h.CreatedAtUtc)
            .IsRequired();

        builder.Property(h => h.UpdatedAtUtc)
            .IsRequired();

        builder.Property(h => h.Version)
            .IsRowVersion();

        builder.HasIndex(h => h.PatientId);
        builder.HasIndex(h => h.Status);
        builder.HasIndex(h => h.DestinationArea);
        builder.HasIndex(h => h.HandedOverAtUtc);
    }
}
