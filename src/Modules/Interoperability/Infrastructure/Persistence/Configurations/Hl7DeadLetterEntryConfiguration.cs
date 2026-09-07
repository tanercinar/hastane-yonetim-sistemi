using HospitalManagement.Modules.Interoperability.Domain.Hl7;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Interoperability.Infrastructure.Persistence.Configurations;

public sealed class Hl7DeadLetterEntryConfiguration : IEntityTypeConfiguration<Hl7DeadLetterEntry>
{
    public void Configure(EntityTypeBuilder<Hl7DeadLetterEntry> builder)
    {
        builder.ToTable("hl7_dead_letter_entries", "interoperability");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MessageControlId)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => e.MessageControlId);

        builder.Property(e => e.MessageType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.FailureReason)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(e => e.PayloadSummary)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(e => e.ReceivedAtUtc)
            .IsRequired();

        builder.Property(e => e.RetryCount)
            .IsRequired();

        builder.Property(e => e.IsResolved)
            .IsRequired();

        builder.Property(e => e.ResolvedAtUtc);
    }
}
