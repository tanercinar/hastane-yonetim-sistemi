using HospitalManagement.UI.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace HospitalManagement.Maui.Services;

public sealed class MauiPlatformInfoService : IPlatformInfoService
{
    public string PlatformName
    {
        get
        {
            if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
            {
                return "Windows";
            }

            if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            {
                return "Android";
            }

            return DeviceInfo.Current.Platform.ToString();
        }
    }

    public bool IsNative => true;

    public string DeviceIdiom
    {
        get
        {
            if (DeviceInfo.Current.Idiom == Microsoft.Maui.Devices.DeviceIdiom.Desktop)
            {
                return "Desktop";
            }

            if (DeviceInfo.Current.Idiom == Microsoft.Maui.Devices.DeviceIdiom.Phone)
            {
                return "Mobile";
            }

            if (DeviceInfo.Current.Idiom == Microsoft.Maui.Devices.DeviceIdiom.Tablet)
            {
                return "Tablet";
            }

            return DeviceInfo.Current.Idiom.ToString();
        }
    }

    public string ApplicationVersion => AppInfo.Current.VersionString;
}

public sealed class MauiPlatformConnectivityService : IPlatformConnectivityService, IDisposable
{
    public MauiPlatformConnectivityService()
    {
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    public bool IsConnected =>
        Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler<bool>? ConnectivityChanged;

    public Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken = default)
    {
        var hasInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        return Task.FromResult(hasInternet);
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var isOnline = e.NetworkAccess == NetworkAccess.Internet;
        ConnectivityChanged?.Invoke(this, isOnline);
    }

    public void Dispose()
    {
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
    }
}

public sealed class MauiPlatformSecureStorage : IAppSecureStorage
{
    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        await SecureStorage.Default.SetAsync(key, value);
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return await SecureStorage.Default.GetAsync(key);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        SecureStorage.Default.Remove(key);
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        SecureStorage.Default.RemoveAll();
        return Task.CompletedTask;
    }
}

public sealed class MauiPlatformNotificationService : IPlatformNotificationService
{
    public event Action<string, string, string>? NotificationReceived;

    public Task ShowInAppNotificationAsync(
        string title,
        string message,
        string severity = "Info",
        CancellationToken cancellationToken = default)
    {
        // Safe in-app notification without logging PHI or posting to OS lock screen
        NotificationReceived?.Invoke(title, message, severity);
        return Task.CompletedTask;
    }
}

public sealed class MauiPlatformBrowserService : IPlatformBrowserService
{
    public async Task OpenSystemBrowserAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        await Launcher.Default.OpenAsync(uri);
    }
}
