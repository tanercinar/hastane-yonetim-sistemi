using HospitalManagement.Modules.Inpatient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Configurations;

public sealed class MedicationAdministrationConfiguration : IEntityTypeConfiguration<MedicationAdministration>
{
    public void Configure(EntityTypeBuilder<MedicationAdministration> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("medication_administrations", "inpatient");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.AdmissionId)
            .IsRequired();

        builder.Property(m => m.PatientId)
            .IsRequired();

        builder.Property(m => m.MedicationName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.Dose)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(m => m.Route)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(m => m.ScheduledTimeUtc)
            .IsRequired();

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(m => m.Reason)
            .HasMaxLength(500);

        builder.Property(m => m.Notes)
            .HasMaxLength(500);

        builder.Property(m => m.Version)
            .IsConcurrencyToken();

        builder.HasIndex(m => m.AdmissionId);
        builder.HasIndex(m => m.PatientId);
        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.ScheduledTimeUtc);
    }
}
