using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Configurations;

public sealed class DeliveryRecordConfiguration : IEntityTypeConfiguration<DeliveryRecord>
{
    public void Configure(EntityTypeBuilder<DeliveryRecord> builder)
    {
        builder.ToTable("DeliveryRecords", "specialty");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.DeliveryProtocolNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(d => d.DeliveryProtocolNumber)
            .IsUnique();

        builder.Property(d => d.MotherPatientId)
            .IsRequired();

        builder.Property(d => d.DeliveryMode)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(d => d.DeliveryTimeUtc)
            .IsRequired();

        builder.Property(d => d.GestationalAgeWeeks)
            .IsRequired();

        builder.Property(d => d.GestationalAgeDays)
            .IsRequired();

        builder.Property(d => d.PerinealTear)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(d => d.EstimatedBloodLossMl)
            .HasPrecision(6, 1);

        builder.Property(d => d.AttendingDoctorId)
            .IsRequired();

        builder.Property(d => d.MaternalComplicationsNotes)
            .HasMaxLength(2000);

        builder.Property(d => d.DeliverySummaryNotes)
            .HasMaxLength(2000);

        builder.Property(d => d.CreatedAtUtc)
            .IsRequired();

        builder.Property(d => d.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(d => d.MotherPatientId);
        builder.HasIndex(d => d.PregnancyEpisodeId);
        builder.HasIndex(d => d.DeliveryTimeUtc);

        builder.HasMany(d => d.Newborns)
            .WithOne()
            .HasForeignKey(n => n.DeliveryRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
