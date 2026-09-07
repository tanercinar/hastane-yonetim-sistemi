namespace HospitalManagement.UI.Services;

/// <summary>
/// Provides secure, encrypted storage abstraction for authentication tokens and sensitive credentials.
/// Implemented using DPAPI on Windows and Android Keystore on Android.
/// </summary>
public interface IAppSecureStorage
{
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);

    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
