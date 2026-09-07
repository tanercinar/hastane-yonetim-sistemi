using HospitalManagement.Modules.ClinicalRecords.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Configurations;

public sealed class ClinicalNoteEntityTypeConfiguration : IEntityTypeConfiguration<ClinicalNote>
{
    public void Configure(EntityTypeBuilder<ClinicalNote> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("clinical_notes", ClinicalRecordsDbContext.Schema);

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasColumnName("id");

        builder.Property(n => n.EncounterId)
            .IsRequired()
            .HasColumnName("encounter_id");

        builder.Property(n => n.PatientId)
            .IsRequired()
            .HasColumnName("patient_id");

        builder.Property(n => n.AuthorPractitionerId)
            .IsRequired()
            .HasColumnName("author_practitioner_id");

        builder.Property(n => n.NoteType)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("note_type");

        builder.Property(n => n.Status)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("status");

        builder.Property(n => n.Title)
            .HasMaxLength(256)
            .IsRequired()
            .HasColumnName("title");

        builder.Property(n => n.ChiefComplaint)
            .HasMaxLength(2000)
            .IsRequired(false)
            .HasColumnName("chief_complaint");

        builder.Property(n => n.HistoryOfPresentIllness)
            .HasMaxLength(4000)
            .IsRequired(false)
            .HasColumnName("history_of_present_illness");

        builder.Property(n => n.PhysicalExamination)
            .HasMaxLength(4000)
            .IsRequired(false)
            .HasColumnName("physical_examination");

        builder.Property(n => n.Assessment)
            .HasMaxLength(4000)
            .IsRequired(false)
            .HasColumnName("assessment");

        builder.Property(n => n.Plan)
            .HasMaxLength(4000)
            .IsRequired(false)
            .HasColumnName("plan");

        builder.Property(n => n.Content)
            .HasMaxLength(8000)
            .IsRequired(false)
            .HasColumnName("content");

        builder.Property(n => n.SignedAtUtc)
            .IsRequired(false)
            .HasColumnName("signed_at_utc");

        builder.Property(n => n.SignedByPractitionerId)
            .IsRequired(false)
            .HasColumnName("signed_by_practitioner_id");

        builder.Property(n => n.ParentNoteId)
            .IsRequired(false)
            .HasColumnName("parent_note_id");

        builder.Property(n => n.CorrectionReason)
            .HasMaxLength(500)
            .IsRequired(false)
            .HasColumnName("correction_reason");

        builder.Property(n => n.EnteredInErrorReason)
            .HasMaxLength(500)
            .IsRequired(false)
            .HasColumnName("entered_in_error_reason");

        builder.Property(n => n.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at_utc");

        builder.Property(n => n.UpdatedAtUtc)
            .IsRequired(false)
            .HasColumnName("updated_at_utc");

        builder.Property(n => n.Version)
            .IsConcurrencyToken()
            .IsRequired()
            .HasColumnName("version");

        builder.HasIndex(n => n.EncounterId)
            .HasDatabaseName("ix_clinical_notes_encounter_id");

        builder.HasIndex(n => n.PatientId)
            .HasDatabaseName("ix_clinical_notes_patient_id");

        builder.HasIndex(n => n.ParentNoteId)
            .HasDatabaseName("ix_clinical_notes_parent_note_id");
    }
}
