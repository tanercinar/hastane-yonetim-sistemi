using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence.Configurations;

public sealed class IcuAdmissionConfiguration : IEntityTypeConfiguration<IcuAdmission>
{
    public void Configure(EntityTypeBuilder<IcuAdmission> builder)
    {
        builder.ToTable("IcuAdmissions", "surgery");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AdmissionProtocolNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(a => a.AdmissionProtocolNumber)
            .IsUnique();

        builder.Property(a => a.InpatientStayId)
            .IsRequired();

        builder.Property(a => a.PatientId)
            .IsRequired();

        builder.Property(a => a.IcuBedId)
            .IsRequired();

        builder.Property(a => a.IcuBedCode)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(a => a.AttendingDoctorId)
            .IsRequired();

        builder.Property(a => a.AdmissionReason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.AcuityLevel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(a => a.VentilationMode)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(a => a.CarePlanNotes)
            .HasMaxLength(4000);

        builder.Property(a => a.DischargeNotes)
            .HasMaxLength(4000);

        builder.Property(a => a.AdmittedAtUtc)
            .IsRequired();

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

        builder.Property(a => a.UpdatedAtUtc)
            .IsRequired();

        builder.Property(a => a.Version)
            .IsRowVersion();

        builder.HasIndex(a => a.PatientId);
        builder.HasIndex(a => a.InpatientStayId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.IcuBedId);
    }
}
