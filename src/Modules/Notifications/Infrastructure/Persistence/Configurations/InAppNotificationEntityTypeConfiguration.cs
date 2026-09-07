using HospitalManagement.Modules.Notifications.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Notifications.Infrastructure.Persistence.Configurations;

public sealed class InAppNotificationEntityTypeConfiguration
    : IEntityTypeConfiguration<InAppNotification>
{
    public void Configure(EntityTypeBuilder<InAppNotification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("in_app_notifications", NotificationsDbContext.Schema);

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasColumnName("id");

        builder.Property(n => n.RecipientPersonId)
            .IsRequired()
            .HasColumnName("recipient_person_id");

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("title");

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(2048)
            .HasColumnName("message");

        builder.Property(n => n.ActionUrl)
            .HasMaxLength(512)
            .HasColumnName("action_url");

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasColumnName("is_read");

        builder.Property(n => n.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at_utc");

        builder.Property(n => n.ReadAtUtc)
            .HasColumnName("read_at_utc");

        builder.Property(n => n.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(128)
            .HasColumnName("idempotency_key");

        builder.HasIndex(n => new { n.RecipientPersonId, n.CreatedAtUtc })
            .HasDatabaseName("ix_in_app_notifications_recipient_created");

        builder.HasIndex(n => n.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_in_app_notifications_idempotency_key");
    }
}
