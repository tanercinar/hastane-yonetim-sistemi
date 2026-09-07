using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Configurations;

public sealed class AntenatalVisitConfiguration : IEntityTypeConfiguration<AntenatalVisit>
{
    public void Configure(EntityTypeBuilder<AntenatalVisit> builder)
    {
        builder.ToTable("AntenatalVisits", "specialty");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .ValueGeneratedNever();

        builder.Property(v => v.PregnancyEpisodeId)
            .IsRequired();

        builder.Property(v => v.EncounterId)
            .IsRequired();

        builder.Property(v => v.VisitDateUtc)
            .IsRequired();

        builder.Property(v => v.GestationalAgeWeeks)
            .IsRequired();

        builder.Property(v => v.GestationalAgeDays)
            .IsRequired();

        builder.Property(v => v.MaternalWeightKg)
            .HasPrecision(5, 2);

        builder.Property(v => v.FundalHeightCm)
            .HasPrecision(4, 1);

        builder.Property(v => v.FetalPresentation)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(v => v.EdemaLevel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(v => v.StaffId)
            .IsRequired();

        builder.Property(v => v.ClinicalNotes)
            .HasMaxLength(2000);

        builder.Property(v => v.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(v => v.PregnancyEpisodeId);
        builder.HasIndex(v => v.EncounterId);
        builder.HasIndex(v => v.VisitDateUtc);
    }
}
