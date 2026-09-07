using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence.Configurations;

public sealed class PerioperativeCorrectionConfiguration : IEntityTypeConfiguration<PerioperativeCorrection>
{
    public void Configure(EntityTypeBuilder<PerioperativeCorrection> builder)
    {
        builder.ToTable("PerioperativeCorrections", "surgery");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.PerioperativeRecordId)
            .IsRequired();

        builder.Property(c => c.CorrectedByDoctorId)
            .IsRequired();

        builder.Property(c => c.ReasonForCorrection)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.CorrectionNote)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(c => c.CorrectedAtUtc)
            .IsRequired();

        builder.HasIndex(c => c.PerioperativeRecordId);
    }
}
