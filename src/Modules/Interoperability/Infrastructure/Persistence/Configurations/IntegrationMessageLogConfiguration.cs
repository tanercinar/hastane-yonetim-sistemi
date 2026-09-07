using HospitalManagement.Modules.Interoperability.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Interoperability.Infrastructure.Persistence.Configurations;

public sealed class IntegrationMessageLogConfiguration : IEntityTypeConfiguration<IntegrationMessageLog>
{
    public void Configure(EntityTypeBuilder<IntegrationMessageLog> builder)
    {
        builder.ToTable("integration_message_logs", "interoperability");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.CorrelationId)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(l => l.CorrelationId);

        builder.Property(l => l.SystemType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.Direction)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.ActionName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.PayloadSummary)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(l => l.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(l => l.RetryCount)
            .IsRequired();

        builder.Property(l => l.DurationMs)
            .IsRequired();

        builder.Property(l => l.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(l => l.TimestampUtc)
            .IsRequired();

        builder.HasIndex(l => l.TimestampUtc);
    }
}
