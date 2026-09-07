using HospitalManagement.Modules.Notifications.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Notifications.Infrastructure.Persistence.Configurations;

public sealed class MockDeliveryRecordEntityTypeConfiguration
    : IEntityTypeConfiguration<MockDeliveryRecord>
{
    public void Configure(EntityTypeBuilder<MockDeliveryRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("mock_deliveries", NotificationsDbContext.Schema);

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id");

        builder.Property(d => d.Channel)
            .IsRequired()
            .HasMaxLength(32)
            .HasColumnName("channel");

        builder.Property(d => d.Recipient)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("recipient");

        builder.Property(d => d.Subject)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("subject");

        builder.Property(d => d.Body)
            .IsRequired()
            .HasMaxLength(2048)
            .HasColumnName("body");

        builder.Property(d => d.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(128)
            .HasColumnName("idempotency_key");

        builder.Property(d => d.SentAtUtc)
            .IsRequired()
            .HasColumnName("sent_at_utc");

        builder.Property(d => d.TemplateKey)
            .IsRequired()
            .HasMaxLength(64)
            .HasColumnName("template_key");

        builder.Property(d => d.Locale)
            .IsRequired()
            .HasMaxLength(10)
            .HasColumnName("locale");

        builder.Property(d => d.Provider)
            .IsRequired()
            .HasMaxLength(64)
            .HasColumnName("provider");

        builder.Property(d => d.AttemptCount)
            .HasColumnName("attempt_count");

        builder.HasIndex(d => new { d.IdempotencyKey, d.Channel })
            .IsUnique()
            .HasDatabaseName("ux_mock_deliveries_idempotency_channel");

        builder.HasIndex(d => new { d.Recipient, d.SentAtUtc })
            .HasDatabaseName("ix_mock_deliveries_recipient_sent");
    }
}
