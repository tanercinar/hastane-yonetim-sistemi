using HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Configurations;

public sealed class DentalExaminationConfiguration : IEntityTypeConfiguration<DentalExaminationRecord>
{
    public void Configure(EntityTypeBuilder<DentalExaminationRecord> builder)
    {
        builder.ToTable("DentalExaminations", "specialty");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.PatientId)
            .IsRequired();

        builder.Property(e => e.ExaminationProtocolNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(e => e.ExaminationProtocolNumber)
            .IsUnique();

        builder.Property(e => e.DentistId)
            .IsRequired();

        builder.Property(e => e.ExaminationDateUtc)
            .IsRequired();

        builder.Property(e => e.ChiefComplaint)
            .HasMaxLength(1000);

        builder.Property(e => e.DiagnosisNotes)
            .HasMaxLength(2000);

        builder.Property(e => e.TreatmentPlanSummary)
            .HasMaxLength(2000);

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(e => e.PatientId);
        builder.HasIndex(e => e.ExaminationDateUtc);
    }
}
