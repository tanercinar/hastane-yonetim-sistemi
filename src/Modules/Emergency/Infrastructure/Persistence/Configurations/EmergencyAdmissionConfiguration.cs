using HospitalManagement.Modules.Emergency.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Emergency.Infrastructure.Persistence.Configurations;

public sealed class EmergencyAdmissionConfiguration : IEntityTypeConfiguration<EmergencyAdmission>
{
    public void Configure(EntityTypeBuilder<EmergencyAdmission> builder)
    {
        builder.ToTable("EmergencyAdmissions", "emergency");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EmergencyProtocolNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(a => a.EmergencyProtocolNumber)
            .IsUnique();

        builder.Property(a => a.PatientId)
            .IsRequired();

        builder.Property(a => a.ArrivalType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(a => a.ChiefComplaint)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.AdmissionNotes)
            .HasMaxLength(2000);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(a => a.AdmittedAtUtc)
            .IsRequired();

        builder.Property(a => a.AdmittingStaffId)
            .IsRequired();

        builder.Property(a => a.AssignedBedOrZone)
            .HasMaxLength(128);

        builder.Property(a => a.DischargeOrDispositionNotes)
            .HasMaxLength(2000);

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

        builder.Property(a => a.UpdatedAtUtc)
            .IsRequired();

        builder.Property(a => a.Version)
            .IsRowVersion();

        builder.OwnsOne(a => a.Triage, triage =>
        {
            triage.Property(t => t.TriageLevel)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasColumnName("TriageLevel");

            triage.Property(t => t.TriageCategoryReason)
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnName("TriageCategoryReason");

            triage.Property(t => t.TriagedAtUtc)
                .IsRequired()
                .HasColumnName("TriagedAtUtc");

            triage.Property(t => t.TriageNurseId)
                .IsRequired()
                .HasColumnName("TriageNurseId");

            triage.Property(t => t.EducationalClassificationAssisted)
                .IsRequired()
                .HasColumnName("EducationalClassificationAssisted");

            triage.Property(t => t.SystolicBp).HasColumnName("SystolicBp");
            triage.Property(t => t.DiastolicBp).HasColumnName("DiastolicBp");
            triage.Property(t => t.HeartRate).HasColumnName("HeartRate");
            triage.Property(t => t.BodyTemperatureCelsius).HasPrecision(4, 1).HasColumnName("BodyTemperatureCelsius");
            triage.Property(t => t.RespiratoryRate).HasColumnName("RespiratoryRate");
            triage.Property(t => t.OxygenSaturationPercent).HasColumnName("OxygenSaturationPercent");
            triage.Property(t => t.PainScale).HasColumnName("PainScale");
            triage.Property(t => t.Consciousness).HasMaxLength(64).HasColumnName("Consciousness");
            triage.Property(t => t.ClinicalNotes).HasMaxLength(2000).HasColumnName("TriageClinicalNotes");
        });

        builder.OwnsOne(a => a.Disposition, disp =>
        {
            disp.Property(d => d.DispositionType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasColumnName("DispositionType");

            disp.Property(d => d.DecidedByDoctorId)
                .IsRequired()
                .HasColumnName("DispositionDecidedByDoctorId");

            disp.Property(d => d.DecidedAtUtc)
                .IsRequired()
                .HasColumnName("DispositionDecidedAtUtc");

            disp.Property(d => d.TargetWardOrIcuId)
                .HasColumnName("DispositionTargetWardOrIcuId");

            disp.Property(d => d.TargetDepartmentName)
                .HasMaxLength(128)
                .HasColumnName("DispositionTargetDepartmentName");

            disp.Property(d => d.DispositionSummaryNotes)
                .IsRequired()
                .HasMaxLength(2000)
                .HasColumnName("DispositionSummaryNotes");

            disp.Property(d => d.FollowUpInstructions)
                .HasMaxLength(2000)
                .HasColumnName("DispositionFollowUpInstructions");
        });

        builder.HasIndex(a => new { a.PatientId, a.Status });
        builder.HasIndex(a => a.Status);
    }
}
