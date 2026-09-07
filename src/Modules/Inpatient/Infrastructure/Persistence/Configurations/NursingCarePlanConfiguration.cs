using HospitalManagement.Modules.Inpatient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Configurations;

public sealed class NursingCarePlanConfiguration : IEntityTypeConfiguration<NursingCarePlan>
{
    public void Configure(EntityTypeBuilder<NursingCarePlan> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("nursing_care_plans", "inpatient");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.AdmissionId)
            .IsRequired();

        builder.Property(p => p.PatientId)
            .IsRequired();

        builder.Property(p => p.CreatedByNurseId)
            .IsRequired();

        builder.Property(p => p.NursingDiagnosis)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.Goal)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(p => p.ResolutionNotes)
            .HasMaxLength(500);

        builder.Property(p => p.Version)
            .IsConcurrencyToken();

        builder.HasMany(p => p.Tasks)
            .WithOne()
            .HasForeignKey(t => t.CarePlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.AdmissionId);
        builder.HasIndex(p => p.PatientId);
        builder.HasIndex(p => p.Status);
    }
}
