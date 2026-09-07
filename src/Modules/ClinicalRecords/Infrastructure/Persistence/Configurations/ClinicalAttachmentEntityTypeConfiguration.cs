using HospitalManagement.Modules.ClinicalRecords.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Configurations;

public sealed class ClinicalAttachmentEntityTypeConfiguration : IEntityTypeConfiguration<ClinicalAttachment>
{
    public void Configure(EntityTypeBuilder<ClinicalAttachment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("clinical_attachments", ClinicalRecordsDbContext.Schema);

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.EncounterId)
            .IsRequired()
            .HasColumnName("encounter_id");

        builder.Property(c => c.PatientId)
            .IsRequired()
            .HasColumnName("patient_id");

        builder.Property(c => c.UploadedByPractitionerId)
            .IsRequired()
            .HasColumnName("uploaded_by_practitioner_id");

        builder.Property(c => c.AttachmentType)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("attachment_type");

        builder.Property(c => c.FileName)
            .HasMaxLength(256)
            .IsRequired()
            .HasColumnName("file_name");

        builder.Property(c => c.StorageKey)
            .HasMaxLength(512)
            .IsRequired()
            .HasColumnName("storage_key");

        builder.Property(c => c.ContentType)
            .HasMaxLength(128)
            .IsRequired()
            .HasColumnName("content_type");

        builder.Property(c => c.ByteSize)
            .IsRequired()
            .HasColumnName("byte_size");

        builder.Property(c => c.Sha256Checksum)
            .HasMaxLength(64)
            .IsRequired()
            .HasColumnName("sha256_checksum");

        builder.Property(c => c.Description)
            .HasMaxLength(1000)
            .IsRequired(false)
            .HasColumnName("description");

        builder.Property(c => c.UploadedAtUtc)
            .IsRequired()
            .HasColumnName("uploaded_at_utc");

        builder.Property(c => c.IsEnteredInError)
            .IsRequired()
            .HasColumnName("is_entered_in_error");

        builder.Property(c => c.EnteredInErrorReason)
            .HasMaxLength(500)
            .IsRequired(false)
            .HasColumnName("entered_in_error_reason");

        builder.Property(c => c.UpdatedAtUtc)
            .IsRequired(false)
            .HasColumnName("updated_at_utc");

        builder.Property(c => c.Version)
            .IsConcurrencyToken()
            .IsRequired()
            .HasColumnName("version");

        builder.HasIndex(c => c.EncounterId)
            .HasDatabaseName("ix_clinical_attachments_encounter_id");

        builder.HasIndex(c => c.PatientId)
            .HasDatabaseName("ix_clinical_attachments_patient_id");

        builder.HasIndex(c => c.Sha256Checksum)
            .HasDatabaseName("ix_clinical_attachments_sha256_checksum");
    }
}
