using HospitalManagement.Modules.Pharmacy.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence.Configurations;

public sealed class PrescriptionEntityTypeConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("prescriptions", PharmacyDbContext.SchemaName);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.PrescriptionNumber)
            .HasColumnName("prescription_number")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(p => p.PatientId)
            .HasColumnName("patient_id")
            .IsRequired();

        builder.Property(p => p.EncounterId)
            .HasColumnName("encounter_id")
            .IsRequired();

        builder.Property(p => p.PrescribingDoctorId)
            .HasColumnName("prescribing_doctor_id")
            .IsRequired();

        builder.Property(p => p.DepartmentId)
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.ValidUntilUtc)
            .HasColumnName("valid_until_utc");

        builder.Property(p => p.SignedAtUtc)
            .HasColumnName("signed_at_utc");

        builder.Property(p => p.SignedByDoctorId)
            .HasColumnName("signed_by_doctor_id");

        builder.Property(p => p.CancelledAtUtc)
            .HasColumnName("cancelled_at_utc");

        builder.Property(p => p.CancelledByDoctorId)
            .HasColumnName("cancelled_by_doctor_id");

        builder.Property(p => p.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasMaxLength(512);

        builder.Property(p => p.EnteredInErrorAtUtc)
            .HasColumnName("entered_in_error_at_utc");

        builder.Property(p => p.EnteredInErrorByDoctorId)
            .HasColumnName("entered_in_error_by_doctor_id");

        builder.Property(p => p.EnteredInErrorReason)
            .HasColumnName("entered_in_error_reason")
            .HasMaxLength(512);

        builder.Property(p => p.DiagnosisSummary)
            .HasColumnName("diagnosis_summary")
            .HasMaxLength(256);

        builder.Property(p => p.GeneralInstructions)
            .HasColumnName("general_instructions")
            .HasMaxLength(1024);

        builder.Property(p => p.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasMany(p => p.Items)
            .WithOne()
            .HasForeignKey(i => i.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.PrescriptionNumber)
            .IsUnique()
            .HasDatabaseName("ux_prescriptions_prescription_number");

        builder.HasIndex(p => p.PatientId)
            .HasDatabaseName("ix_prescriptions_patient_id");

        builder.HasIndex(p => p.EncounterId)
            .HasDatabaseName("ix_prescriptions_encounter_id");

        builder.HasIndex(p => p.PrescribingDoctorId)
            .HasDatabaseName("ix_prescriptions_prescribing_doctor_id");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("ix_prescriptions_status");
    }
}
