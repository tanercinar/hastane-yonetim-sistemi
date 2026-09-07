using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Configurations;

public sealed class PregnancyEpisodeConfiguration : IEntityTypeConfiguration<PregnancyEpisode>
{
    public void Configure(EntityTypeBuilder<PregnancyEpisode> builder)
    {
        builder.ToTable("PregnancyEpisodes", "specialty");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.EpisodeProtocolNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(p => p.EpisodeProtocolNumber)
            .IsUnique();

        builder.Property(p => p.PatientId)
            .IsRequired();

        builder.Property(p => p.OpeningEncounterId)
            .IsRequired();

        builder.Property(p => p.BloodGroupAndRh)
            .HasMaxLength(16);

        builder.Property(p => p.RiskCategory)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(p => p.RiskFactorsNotes)
            .HasMaxLength(2000);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(p => p.LastMenstrualPeriodUtc)
            .IsRequired();

        builder.Property(p => p.EstimatedDeliveryDateUtc)
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(p => p.PatientId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.OpeningEncounterId);
        builder.HasIndex(p => p.RiskCategory);
        builder.HasIndex(p => p.EstimatedDeliveryDateUtc);

        builder.HasMany(p => p.AntenatalVisits)
            .WithOne()
            .HasForeignKey(v => v.PregnancyEpisodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
