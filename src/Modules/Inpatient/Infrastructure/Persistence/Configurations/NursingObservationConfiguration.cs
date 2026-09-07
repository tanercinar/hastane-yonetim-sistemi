using HospitalManagement.Modules.Inpatient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Configurations;

public sealed class NursingObservationConfiguration : IEntityTypeConfiguration<NursingObservation>
{
    public void Configure(EntityTypeBuilder<NursingObservation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("nursing_observations", "inpatient");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedNever();

        builder.Property(o => o.AdmissionId)
            .IsRequired();

        builder.Property(o => o.PatientId)
            .IsRequired();

        builder.Property(o => o.RecordedByNurseId)
            .IsRequired();

        builder.Property(o => o.ObservedAtUtc)
            .IsRequired();

        builder.Property(o => o.BodyTemperatureCelsius)
            .HasPrecision(4, 1);

        builder.Property(o => o.Consciousness)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(o => o.ClinicalNotes)
            .HasMaxLength(1000);

        builder.Property(o => o.CorrectionReason)
            .HasMaxLength(500);

        builder.Property(o => o.Version)
            .IsConcurrencyToken();

        builder.HasIndex(o => o.AdmissionId);
        builder.HasIndex(o => o.PatientId);
        builder.HasIndex(o => o.ObservedAtUtc);
    }
}
