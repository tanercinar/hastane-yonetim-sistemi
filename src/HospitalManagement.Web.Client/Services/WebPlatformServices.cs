using System.Collections.Concurrent;
using HospitalManagement.UI.Services;
using Microsoft.AspNetCore.Components;

namespace HospitalManagement.Web.Client.Services;

public sealed class WebPlatformInfoService : IPlatformInfoService
{
    public string PlatformName => "Web";

    public bool IsNative => false;

    public string DeviceIdiom => "Browser";

    public string ApplicationVersion => "1.0.0-web";
}

public sealed class WebPlatformConnectivityService : IPlatformConnectivityService
{
    public bool IsConnected => true; // WebAssembly client is always connected if running

    public event EventHandler<bool>? ConnectivityChanged
    {
        add
        {
        }
        remove
        {
        }
    }

    public Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}

public sealed class WebPlatformSecureStorage : IAppSecureStorage
{
    // Web does not store tokens in local/session storage per ADR-0004.
    // Uses in-memory transient dictionary.
    private readonly ConcurrentDictionary<string, string> _inMemoryStore = new(StringComparer.Ordinal);

    public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        _inMemoryStore[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _inMemoryStore.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _inMemoryStore.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _inMemoryStore.Clear();
        return Task.CompletedTask;
    }
}

public sealed class WebPlatformNotificationService : IPlatformNotificationService
{
    public Task ShowInAppNotificationAsync(
        string title,
        string message,
        string severity = "Info",
        CancellationToken cancellationToken = default)
    {
        // In-app web notification (can trigger UI toast or console log)
        return Task.CompletedTask;
    }
}

public sealed class WebPlatformBrowserService(NavigationManager navigationManager) : IPlatformBrowserService
{
    private readonly NavigationManager _navigationManager = navigationManager;

    public Task OpenSystemBrowserAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        _navigationManager.NavigateTo(uri.ToString(), forceLoad: true);
        return Task.CompletedTask;
    }
}
