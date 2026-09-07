using HospitalManagement.Modules.Notifications.Application;
using HospitalManagement.Modules.Notifications.Domain;
using HospitalManagement.Modules.Notifications.Infrastructure;

namespace HospitalManagement.UnitTests;

public sealed class NotificationDomainUnitTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G07")]
    public void OutboxEventCreationAndStatusTransitionsWork()
    {
        var now = DateTime.UtcNow;
        var evt = NotificationOutboxEvent.Create(
            Guid.NewGuid(),
            "Appointment.Booked",
            "appt-booked-12345",
            Guid.NewGuid(),
            "DEMO-patient@hospital.invalid",
            "+905550000001",
            "Randevunuz Onaylandı",
            "Sayın hastamız, 28.08.2026 10:00 randevunuz onaylanmıştır.",
            now);

        Assert.Equal("Pending", evt.Status);
        Assert.Equal(0, evt.RetryCount);
        Assert.Null(evt.ProcessedAtUtc);
        Assert.Equal("appt-booked-12345", evt.IdempotencyKey);

        // Mark Failed
        evt.MarkFailed("SMTP bağlantı hatası");
        Assert.Equal("Failed", evt.Status);
        Assert.Equal(1, evt.RetryCount);
        Assert.Equal("SMTP bağlantı hatası", evt.Error);

        // Mark Processed
        var processedTime = now.AddMinutes(5);
        evt.MarkProcessed(processedTime);
        Assert.Equal("Processed", evt.Status);
        Assert.Equal(processedTime, evt.ProcessedAtUtc);
        Assert.Null(evt.Error);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G07")]
    public void InAppNotificationCreationAndMarkAsReadWorks()
    {
        var now = DateTime.UtcNow;
        var notif = InAppNotification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Muayene Girişiniz Yapıldı",
            "Sıra Numaranız: #3",
            "/patient/appointments",
            "appt-checkin-12345-3",
            now);

        Assert.False(notif.IsRead);
        Assert.Null(notif.ReadAtUtc);

        var readTime = now.AddMinutes(10);
        notif.MarkAsRead(readTime);

        Assert.True(notif.IsRead);
        Assert.Equal(readTime, notif.ReadAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G07")]
    public void OutboxMessageConformsToPrivacyAndDoesNotLeakSensitiveClinicalData()
    {
        var now = DateTime.UtcNow;
        var message = "Sayın hastamız, 28.08.2026 14:30 tarihindeki muayene randevunuz onaylanmıştır. Randevu Referansı: DEMO-REF-99.";

        var evt = NotificationOutboxEvent.Create(
            Guid.NewGuid(),
            "Appointment.Booked",
            "appt-booked-demo-99",
            Guid.NewGuid(),
            "DEMO-patient@hospital.invalid",
            null,
            "Randevunuz Onaylandı",
            message,
            now);

        // Verify privacy invariant: No ICD codes, no diagnostic notes, no clinical observations
        Assert.DoesNotContain("ICD", evt.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tanı:", evt.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Klinik Not:", evt.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Randevu Referansı", evt.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G08")]
    public void ExternalTemplateUsesRequestedLocaleAndOnlyApprovedAppointmentTokens()
    {
        var rendered = NotificationTemplateCatalog.RenderExternal(
            "Appointment.Booked",
            "en-US",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["appointmentDate"] = "02.09.2026 10:30",
                ["reference"] = "DEMO-APT-42",
                ["diagnosis"] = "DEMO-GIZLI-TANI",
            });

        Assert.Equal("Your appointment is confirmed", rendered.Subject);
        Assert.Contains("02.09.2026 10:30", rendered.Body, StringComparison.Ordinal);
        Assert.Contains("DEMO-APT-42", rendered.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("DEMO-GIZLI-TANI", rendered.Body, StringComparison.Ordinal);
        Assert.Equal("en-US", rendered.Locale);
        Assert.Equal("Appointment.Booked", rendered.TemplateKey);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G08")]
    public void RecipientCanChooseExternalChannelsAndSupportedLocale()
    {
        var now = DateTime.UtcNow;
        var preference = NotificationPreference.Create(
            Guid.NewGuid(),
            emailEnabled: true,
            smsEnabled: false,
            locale: "tr-TR",
            now);

        preference.Update(
            emailEnabled: false,
            smsEnabled: true,
            locale: "en-US",
            now.AddMinutes(1));

        Assert.False(preference.EmailEnabled);
        Assert.True(preference.SmsEnabled);
        Assert.Equal("en-US", preference.Locale);
        Assert.Equal(now.AddMinutes(1), preference.UpdatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G08")]
    public async Task MockProviderRetriesTransientFailureAndReturnsAttemptCount()
    {
        var transport = new FlakyNotificationTransport(failuresBeforeSuccess: 2);
        var provider = new RetryingNotificationProviderPort(
            transport,
            maxAttempts: 3);

        var result = await provider.DeliverAsync(new NotificationProviderMessage(
            Channel: "Email",
            Recipient: "DEMO-patient@hospital.invalid",
            Subject: "Randevunuz Onaylandı",
            Body: "Güvenli DEMO randevu bildirimi.",
            IdempotencyKey: "DEMO-NOTIFY-42",
            TemplateKey: "Appointment.Booked",
            Locale: "tr-TR"));

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.AttemptCount);
        Assert.Equal(3, transport.AttemptCount);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G08")]
    public async Task MockProviderExhaustsRetryBudgetWithoutLeakingProviderException()
    {
        var transport = new FlakyNotificationTransport(failuresBeforeSuccess: int.MaxValue);
        var provider = new RetryingNotificationProviderPort(
            transport,
            maxAttempts: 3);

        var result = await provider.DeliverAsync(new NotificationProviderMessage(
            Channel: "Sms",
            Recipient: "+900000000000",
            Subject: "Güvenli bildirim",
            Body: "Ayrıntılar için güvenli portala giriş yapın.",
            IdempotencyKey: "DEMO-NOTIFY-FAIL-42",
            TemplateKey: "Notification.Generic",
            Locale: "tr-TR"));

        Assert.False(result.Succeeded);
        Assert.Equal(3, result.AttemptCount);
        Assert.Equal(3, transport.AttemptCount);
        Assert.Equal("MOCK_NOTIFICATION_PROVIDER_FAILED", result.ErrorCode);
        Assert.DoesNotContain("DEMO-GIZLI", result.ErrorCode, StringComparison.Ordinal);
    }

    private sealed class FlakyNotificationTransport(int failuresBeforeSuccess)
        : INotificationTransport
    {
        private readonly int _failuresBeforeSuccess = failuresBeforeSuccess;

        public int AttemptCount
        {
            get; private set;
        }

        public Task CaptureAsync(
            NotificationProviderMessage message,
            CancellationToken cancellationToken = default)
        {
            AttemptCount++;
            if (AttemptCount <= _failuresBeforeSuccess)
            {
                throw new TimeoutException("DEMO-GIZLI-SAGLAYICI-HATASI");
            }

            return Task.CompletedTask;
        }
    }
}
