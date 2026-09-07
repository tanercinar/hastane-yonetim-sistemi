using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence.Configurations;

public sealed class PerioperativeRecordConfiguration : IEntityTypeConfiguration<PerioperativeRecord>
{
    public void Configure(EntityTypeBuilder<PerioperativeRecord> builder)
    {
        builder.ToTable("PerioperativeRecords", "surgery");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.SurgeryBookingId)
            .IsRequired();

        builder.HasIndex(r => r.SurgeryBookingId)
            .IsUnique();

        builder.Property(r => r.PatientId)
            .IsRequired();

        builder.Property(r => r.OperatingRoomId)
            .IsRequired();

        builder.Property(r => r.AnesthesiaType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(r => r.AnesthesiaNotes)
            .HasMaxLength(2000);

        builder.Property(r => r.IntraoperativeFindings)
            .HasMaxLength(4000);

        builder.Property(r => r.IntraoperativeComplications)
            .HasMaxLength(2000);

        builder.Property(r => r.SpecimensCollected)
            .HasMaxLength(1000);

        builder.Property(r => r.PostOpDisposition)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(r => r.PostOpInstructions)
            .HasMaxLength(2000);

        builder.Property(r => r.IsSigned)
            .IsRequired();

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .IsRequired();

        builder.Property(r => r.Version)
            .IsRowVersion();

        builder.HasMany(r => r.Corrections)
            .WithOne()
            .HasForeignKey(c => c.PerioperativeRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Corrections)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
