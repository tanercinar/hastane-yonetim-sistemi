using HospitalManagement.Modules.Emergency.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Emergency.Infrastructure.Persistence.Configurations;

public sealed class EmergencyConsultationConfiguration : IEntityTypeConfiguration<EmergencyConsultation>
{
    public void Configure(EntityTypeBuilder<EmergencyConsultation> builder)
    {
        builder.ToTable("EmergencyConsultations", "emergency");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AdmissionId)
            .IsRequired();

        builder.Property(c => c.DepartmentId)
            .IsRequired();

        builder.Property(c => c.DepartmentName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(c => c.RequestedByDoctorId)
            .IsRequired();

        builder.Property(c => c.RequestedAtUtc)
            .IsRequired();

        builder.Property(c => c.Urgency)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(c => c.ClinicalReason)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(c => c.ConsultationResponseNotes)
            .HasMaxLength(3000);

        builder.Property(c => c.CancellationReason)
            .HasMaxLength(500);

        builder.Property(c => c.Version)
            .IsRowVersion();

        builder.HasIndex(c => new { c.AdmissionId, c.Status });
        builder.HasIndex(c => new { c.DepartmentId, c.Status });
        builder.HasIndex(c => c.RequestedAtUtc);
    }
}
