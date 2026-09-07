using HospitalManagement.Modules.ClinicalRecords.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Configurations;

public sealed class ConsultationRequestEntityTypeConfiguration : IEntityTypeConfiguration<ConsultationRequest>
{
    public void Configure(EntityTypeBuilder<ConsultationRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("consultation_requests", ClinicalRecordsDbContext.Schema);

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.EncounterId)
            .IsRequired()
            .HasColumnName("encounter_id");

        builder.Property(c => c.PatientId)
            .IsRequired()
            .HasColumnName("patient_id");

        builder.Property(c => c.RequestingPractitionerId)
            .IsRequired()
            .HasColumnName("requesting_practitioner_id");

        builder.Property(c => c.TargetDepartmentId)
            .IsRequired()
            .HasColumnName("target_department_id");

        builder.Property(c => c.TargetPractitionerId)
            .IsRequired(false)
            .HasColumnName("target_practitioner_id");

        builder.Property(c => c.AssignedPractitionerId)
            .IsRequired(false)
            .HasColumnName("assigned_practitioner_id");

        builder.Property(c => c.Urgency)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("urgency");

        builder.Property(c => c.Status)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("status");

        builder.Property(c => c.ReasonForConsultation)
            .HasMaxLength(1000)
            .IsRequired()
            .HasColumnName("reason_for_consultation");

        builder.Property(c => c.ClinicalQuestion)
            .HasMaxLength(2000)
            .IsRequired()
            .HasColumnName("clinical_question");

        builder.Property(c => c.ConsultationReport)
            .HasMaxLength(4000)
            .IsRequired(false)
            .HasColumnName("consultation_report");

        builder.Property(c => c.Recommendation)
            .HasMaxLength(2000)
            .IsRequired(false)
            .HasColumnName("recommendation");

        builder.Property(c => c.DeclineReason)
            .HasMaxLength(500)
            .IsRequired(false)
            .HasColumnName("decline_reason");

        builder.Property(c => c.CancellationReason)
            .HasMaxLength(500)
            .IsRequired(false)
            .HasColumnName("cancellation_reason");

        builder.Property(c => c.EnteredInErrorReason)
            .HasMaxLength(500)
            .IsRequired(false)
            .HasColumnName("entered_in_error_reason");

        builder.Property(c => c.RequestedAtUtc)
            .IsRequired()
            .HasColumnName("requested_at_utc");

        builder.Property(c => c.AcceptedAtUtc)
            .IsRequired(false)
            .HasColumnName("accepted_at_utc");

        builder.Property(c => c.CompletedAtUtc)
            .IsRequired(false)
            .HasColumnName("completed_at_utc");

        builder.Property(c => c.UpdatedAtUtc)
            .IsRequired(false)
            .HasColumnName("updated_at_utc");

        builder.Property(c => c.Version)
            .IsConcurrencyToken()
            .IsRequired()
            .HasColumnName("version");

        builder.HasIndex(c => c.EncounterId)
            .HasDatabaseName("ix_consultation_requests_encounter_id");

        builder.HasIndex(c => c.PatientId)
            .HasDatabaseName("ix_consultation_requests_patient_id");

        builder.HasIndex(c => c.TargetDepartmentId)
            .HasDatabaseName("ix_consultation_requests_target_department_id");

        builder.HasIndex(c => c.AssignedPractitionerId)
            .HasDatabaseName("ix_consultation_requests_assigned_practitioner_id");
    }
}
