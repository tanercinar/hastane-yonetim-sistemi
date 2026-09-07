using HospitalManagement.Modules.Notifications.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Notifications.Infrastructure.Persistence.Configurations;

public sealed class NotificationOutboxEventEntityTypeConfiguration
    : IEntityTypeConfiguration<NotificationOutboxEvent>
{
    public void Configure(EntityTypeBuilder<NotificationOutboxEvent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("notification_outbox_events", NotificationsDbContext.Schema);

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(64)
            .HasColumnName("event_type");

        builder.Property(e => e.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(128)
            .HasColumnName("idempotency_key");

        builder.Property(e => e.RecipientPersonId)
            .IsRequired()
            .HasColumnName("recipient_person_id");

        builder.Property(e => e.RecipientEmail)
            .HasMaxLength(256)
            .HasColumnName("recipient_email");

        builder.Property(e => e.RecipientPhone)
            .HasMaxLength(32)
            .HasColumnName("recipient_phone");

        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("subject");

        builder.Property(e => e.Message)
            .IsRequired()
            .HasMaxLength(2048)
            .HasColumnName("message");

        builder.Property(e => e.TemplateKey)
            .IsRequired()
            .HasMaxLength(64)
            .HasColumnName("template_key");

        builder.Property(e => e.TemplateTokensJson)
            .IsRequired()
            .HasMaxLength(2048)
            .HasColumnName("template_tokens_json");

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at_utc");

        builder.Property(e => e.ProcessedAtUtc)
            .HasColumnName("processed_at_utc");

        builder.Property(e => e.RetryCount)
            .IsRequired()
            .HasColumnName("retry_count");

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(32)
            .HasColumnName("status");

        builder.Property(e => e.Error)
            .HasMaxLength(1024)
            .HasColumnName("error");

        builder.HasIndex(e => e.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_notification_outbox_idempotency_key");

        builder.HasIndex(e => new { e.Status, e.CreatedAtUtc })
            .HasDatabaseName("ix_notification_outbox_status_created");
    }
}
