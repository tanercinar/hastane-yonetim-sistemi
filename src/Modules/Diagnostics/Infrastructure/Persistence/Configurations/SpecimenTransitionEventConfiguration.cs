using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

public sealed class SpecimenTransitionEventConfiguration : IEntityTypeConfiguration<SpecimenTransitionEvent>
{
    public void Configure(EntityTypeBuilder<SpecimenTransitionEvent> builder)
    {
        builder.ToTable("specimen_transition_events", "diagnostics");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(t => t.SpecimenId)
            .HasColumnName("specimen_id")
            .IsRequired();

        builder.Property(t => t.FromStatus)
            .HasColumnName("from_status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(t => t.ToStatus)
            .HasColumnName("to_status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(t => t.TransitionedAtUtc)
            .HasColumnName("transitioned_at_utc")
            .IsRequired();

        builder.Property(t => t.ActorUserId)
            .HasColumnName("actor_user_id")
            .IsRequired();

        builder.Property(t => t.ActorRole)
            .HasColumnName("actor_role")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Location)
            .HasColumnName("location")
            .HasMaxLength(150);

        builder.Property(t => t.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        builder.HasIndex(t => t.SpecimenId);
        builder.HasIndex(t => t.TransitionedAtUtc);
    }
}
