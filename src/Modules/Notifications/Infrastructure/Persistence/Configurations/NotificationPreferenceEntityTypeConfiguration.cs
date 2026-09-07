using HospitalManagement.Modules.Notifications.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Notifications.Infrastructure.Persistence.Configurations;

public sealed class NotificationPreferenceEntityTypeConfiguration
    : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("notification_preferences", NotificationsDbContext.Schema);
        builder.HasKey(preference => preference.RecipientPersonId);

        builder.Property(preference => preference.RecipientPersonId)
            .HasColumnName("recipient_person_id");
        builder.Property(preference => preference.EmailEnabled)
            .HasColumnName("email_enabled");
        builder.Property(preference => preference.SmsEnabled)
            .HasColumnName("sms_enabled");
        builder.Property(preference => preference.Locale)
            .IsRequired()
            .HasMaxLength(10)
            .HasColumnName("locale");
        builder.Property(preference => preference.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");
    }
}
