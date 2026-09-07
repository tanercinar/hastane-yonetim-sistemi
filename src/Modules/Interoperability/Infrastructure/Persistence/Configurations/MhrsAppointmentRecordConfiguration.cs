using HospitalManagement.Modules.Interoperability.Domain.Mhrs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Interoperability.Infrastructure.Persistence.Configurations;

public sealed class MhrsAppointmentRecordConfiguration : IEntityTypeConfiguration<MhrsAppointmentRecord>
{
    public void Configure(EntityTypeBuilder<MhrsAppointmentRecord> builder)
    {
        builder.ToTable("mhrs_appointments", "interoperability");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.MhrsAppointmentId)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(a => a.MhrsAppointmentId)
            .IsUnique();

        builder.Property(a => a.SlotId)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(a => a.SlotId);

        builder.Property(a => a.PatientNationalId)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(a => a.PatientNationalId);

        builder.Property(a => a.PatientFullName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.DoctorId)
            .IsRequired();

        builder.Property(a => a.DoctorName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.ClinicName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.AppointmentDateTimeUtc)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.IdempotencyKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(a => a.IdempotencyKey)
            .IsUnique();

        builder.Property(a => a.CancellationReason)
            .HasMaxLength(500);

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

        builder.Property(a => a.UpdatedAtUtc)
            .IsRequired();
    }
}
