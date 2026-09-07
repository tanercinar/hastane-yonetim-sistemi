using HospitalManagement.Modules.Inpatient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Configurations;

public sealed class BedConfiguration : IEntityTypeConfiguration<Bed>
{
    public void Configure(EntityTypeBuilder<Bed> builder)
    {
        builder.ToTable("beds", "inpatient");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BedNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(b => b.GenderConstraint)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(b => b.IsolationType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(b => b.MaintenanceReason)
            .HasMaxLength(512);

        builder.Property(b => b.Version)
            .IsConcurrencyToken();

        builder.HasIndex(b => new { b.RoomId, b.BedNumber })
            .IsUnique();

        builder.HasIndex(b => b.WardId);
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.CurrentAdmissionId);
        builder.HasIndex(b => b.CurrentPatientId);
    }
}
