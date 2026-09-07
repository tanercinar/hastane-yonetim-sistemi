using HospitalManagement.Modules.Inpatient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Configurations;

public sealed class InpatientAdmissionConfiguration : IEntityTypeConfiguration<InpatientAdmission>
{
    public void Configure(EntityTypeBuilder<InpatientAdmission> builder)
    {
        builder.ToTable("admissions", "inpatient");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.AdmissionNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.PatientId)
            .IsRequired();

        builder.Property(a => a.OrderingDoctorId)
            .IsRequired();

        builder.Property(a => a.AttendingDoctorId)
            .IsRequired();

        builder.Property(a => a.DepartmentId)
            .IsRequired();

        builder.Property(a => a.AdmittingWardId)
            .IsRequired();

        builder.Property(a => a.AdmissionReason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.DiagnosisCode)
            .HasMaxLength(32);

        builder.Property(a => a.DiagnosisDescription)
            .HasMaxLength(256);

        builder.Property(a => a.DietType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.IsolationRequired)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(a => a.DischargeSummary)
            .HasMaxLength(2000);

        builder.Property(a => a.CancellationReason)
            .HasMaxLength(500);

        builder.Property(a => a.Version)
            .IsConcurrencyToken();

        builder.HasIndex(a => a.AdmissionNumber)
            .IsUnique();

        builder.HasIndex(a => a.PatientId)
            .HasDatabaseName("IX_admissions_patient_id_active")
            .IsUnique()
            .HasFilter("\"Status\" IN ('Requested', 'Accepted', 'Admitted', 'Transferring')");
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.AdmittingWardId);
        builder.HasIndex(a => a.DepartmentId);
        builder.HasIndex(a => a.AssignedBedId)
            .HasDatabaseName("IX_admissions_assigned_bed_id_active")
            .IsUnique()
            .HasFilter("\"AssignedBedId\" IS NOT NULL AND \"Status\" IN ('Accepted', 'Admitted', 'Transferring')");
        builder.HasIndex(a => a.RequestedAtUtc);
    }
}
