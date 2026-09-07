using HospitalManagement.Modules.Notifications.Application;

namespace HospitalManagement.Modules.Notifications.Infrastructure;

public sealed class RetryingNotificationProviderPort(
    INotificationTransport transport,
    int maxAttempts = 3) : INotificationProviderPort
{
    private readonly INotificationTransport _transport = transport
        ?? throw new ArgumentNullException(nameof(transport));
    private readonly int _maxAttempts = maxAttempts is >= 1 and <= 5
        ? maxAttempts
        : throw new ArgumentOutOfRangeException(nameof(maxAttempts));

    public async Task<NotificationProviderResult> DeliverAsync(
        NotificationProviderMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                await _transport.CaptureAsync(
                    message with
                    {
                        AttemptCount = attempt
                    },
                    cancellationToken);
                return new NotificationProviderResult(true, attempt, null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception) when (attempt < _maxAttempts)
            {
                // Yerel MOCK sağlayıcının geçici hatası kontrollü biçimde yeniden denenir.
            }
            catch (Exception)
            {
                return new NotificationProviderResult(
                    false,
                    attempt,
                    "MOCK_NOTIFICATION_PROVIDER_FAILED");
            }
        }

        return new NotificationProviderResult(
            false,
            _maxAttempts,
            "MOCK_NOTIFICATION_PROVIDER_FAILED");
    }
}
