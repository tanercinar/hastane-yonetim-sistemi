namespace HospitalManagement.UI.Services;

/// <summary>
/// Provides in-app notifications without exposing protected health information (PHI) to OS lock screen.
/// </summary>
public interface IPlatformNotificationService
{
    Task ShowInAppNotificationAsync(
        string title,
        string message,
        string severity = "Info",
        CancellationToken cancellationToken = default);
}
