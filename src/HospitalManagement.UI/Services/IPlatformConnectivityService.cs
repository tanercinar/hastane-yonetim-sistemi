namespace HospitalManagement.UI.Services;

/// <summary>
/// Provides network connectivity monitoring for online-only clinical applications.
/// No offline queueing; exposes explicit network availability.
/// </summary>
public interface IPlatformConnectivityService
{
    bool IsConnected
    {
        get;
    }

    event EventHandler<bool>? ConnectivityChanged;

    Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken = default);
}
