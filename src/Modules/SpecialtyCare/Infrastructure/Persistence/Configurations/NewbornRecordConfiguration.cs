using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Configurations;

public sealed class NewbornRecordConfiguration : IEntityTypeConfiguration<NewbornRecord>
{
    public void Configure(EntityTypeBuilder<NewbornRecord> builder)
    {
        builder.ToTable("NewbornRecords", "specialty");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id)
            .ValueGeneratedNever();

        builder.Property(n => n.DeliveryRecordId)
            .IsRequired();

        builder.Property(n => n.NewbornPatientId)
            .IsRequired();

        builder.Property(n => n.BirthOrder)
            .IsRequired();

        builder.Property(n => n.BirthTimeUtc)
            .IsRequired();

        builder.Property(n => n.Gender)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(n => n.BirthWeightGrams)
            .HasPrecision(6, 1);

        builder.Property(n => n.BirthLengthCm)
            .HasPrecision(4, 1);

        builder.Property(n => n.HeadCircumferenceCm)
            .HasPrecision(4, 1);

        builder.Property(n => n.ApgarScore1Min)
            .IsRequired();

        builder.Property(n => n.ApgarScore5Min)
            .IsRequired();

        builder.Property(n => n.ResuscitationGiven)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(n => n.CordBloodPh)
            .HasMaxLength(16);

        builder.Property(n => n.ComplicationsNotes)
            .HasMaxLength(2000);

        builder.Property(n => n.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(n => n.DeliveryRecordId);
        builder.HasIndex(n => n.NewbornPatientId)
            .IsUnique()
            .HasDatabaseName("UX_NewbornRecords_NewbornPatientId");
        builder.HasIndex(n => n.BirthTimeUtc);
    }
}
