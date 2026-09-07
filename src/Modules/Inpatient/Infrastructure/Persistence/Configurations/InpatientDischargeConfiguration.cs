using HospitalManagement.Modules.Inpatient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Configurations;

public sealed class InpatientDischargeConfiguration : IEntityTypeConfiguration<InpatientDischarge>
{
    public void Configure(EntityTypeBuilder<InpatientDischarge> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("discharges", "inpatient");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.AdmissionId)
            .IsRequired();

        builder.Property(d => d.PatientId)
            .IsRequired();

        builder.Property(d => d.DischargingDoctorId)
            .IsRequired();

        builder.Property(d => d.DischargeType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(d => d.DischargeSummary)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(d => d.FinalDiagnosisCode)
            .HasMaxLength(32);

        builder.Property(d => d.FinalDiagnosisDescription)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.DischargeRecommendations)
            .HasMaxLength(2000);

        builder.Property(d => d.DischargePrescriptionSummary)
            .HasMaxLength(2000);

        builder.Property(d => d.TransferFacilityName)
            .HasMaxLength(256);

        builder.Property(d => d.TransferReason)
            .HasMaxLength(1000);

        builder.Property(d => d.Version)
            .IsConcurrencyToken();

        builder.HasIndex(d => d.AdmissionId)
            .IsUnique();

        builder.HasIndex(d => d.PatientId);
        builder.HasIndex(d => d.DischargedAtUtc);
    }
}
