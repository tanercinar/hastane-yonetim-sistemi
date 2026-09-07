using HospitalManagement.Modules.ClinicalRecords.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Configurations;

public sealed class EncounterParticipantEntityTypeConfiguration : IEntityTypeConfiguration<EncounterParticipant>
{
    public void Configure(EntityTypeBuilder<EncounterParticipant> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("encounter_participants", ClinicalRecordsDbContext.Schema);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.EncounterId)
            .IsRequired()
            .HasColumnName("encounter_id");

        builder.Property(p => p.PractitionerId)
            .IsRequired()
            .HasColumnName("practitioner_id");

        builder.Property(p => p.Role)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("role");

        builder.Property(p => p.JoinedAtUtc)
            .IsRequired()
            .HasColumnName("joined_at_utc");

        builder.Property(p => p.LeftAtUtc)
            .IsRequired(false)
            .HasColumnName("left_at_utc");

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at_utc");

        builder.HasIndex(p => p.EncounterId)
            .HasDatabaseName("ix_encounter_participants_encounter_id");

        builder.HasIndex(p => p.PractitionerId)
            .HasDatabaseName("ix_encounter_participants_practitioner_id");
    }
}
