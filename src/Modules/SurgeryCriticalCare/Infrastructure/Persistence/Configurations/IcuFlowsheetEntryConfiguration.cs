using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence.Configurations;

public sealed class IcuFlowsheetEntryConfiguration : IEntityTypeConfiguration<IcuFlowsheetEntry>
{
    public void Configure(EntityTypeBuilder<IcuFlowsheetEntry> builder)
    {
        builder.ToTable("IcuFlowsheetEntries", "surgery");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IcuAdmissionId)
            .IsRequired();

        builder.Property(e => e.RecordedAtUtc)
            .IsRequired();

        builder.Property(e => e.RecordedByStaffId)
            .IsRequired();

        builder.Property(e => e.OxygenSaturationPct)
            .HasPrecision(5, 2);

        builder.Property(e => e.BodyTemperatureCelsius)
            .HasPrecision(4, 2);

        builder.Property(e => e.VentilationMode)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.ClinicalNotes)
            .HasMaxLength(2000);

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(e => e.IcuAdmissionId);
        builder.HasIndex(e => new { e.IcuAdmissionId, e.RecordedAtUtc });
    }
}
