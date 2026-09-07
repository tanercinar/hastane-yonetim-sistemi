using HospitalManagement.UI.Services;
using HospitalManagement.Web.Client.Services;

using Xunit;

namespace HospitalManagement.ComponentTests.Services;

public sealed class PlatformServicesTests
{
    [Fact]
    public void WebPlatformInfoServiceShouldReturnCorrectWebProperties()
    {
        var service = new WebPlatformInfoService();

        Assert.Equal("Web", service.PlatformName);
        Assert.False(service.IsNative);
        Assert.Equal("Browser", service.DeviceIdiom);
        Assert.NotEmpty(service.ApplicationVersion);
    }

    [Fact]
    public async Task WebPlatformConnectivityServiceShouldReportOnlineByDefault()
    {
        var service = new WebPlatformConnectivityService();

        Assert.True(service.IsConnected);
        var isOnline = await service.CheckConnectivityAsync();
        Assert.True(isOnline);
    }

    [Fact]
    public async Task WebPlatformSecureStorageShouldSupportInMemoryKeyValues()
    {
        var storage = new WebPlatformSecureStorage();

        await storage.SetAsync("auth_test_key", "secret_value_123");
        var value = await storage.GetAsync("auth_test_key");
        Assert.Equal("secret_value_123", value);

        await storage.RemoveAsync("auth_test_key");
        var removedValue = await storage.GetAsync("auth_test_key");
        Assert.Null(removedValue);

        await storage.SetAsync("k1", "v1");
        await storage.SetAsync("k2", "v2");
        await storage.ClearAsync();
        Assert.Null(await storage.GetAsync("k1"));
        Assert.Null(await storage.GetAsync("k2"));
    }

    [Fact]
    public async Task WebPlatformSecureStorageInvalidArgumentsShouldThrow()
    {
        var storage = new WebPlatformSecureStorage();

        await Assert.ThrowsAsync<ArgumentException>(() => storage.SetAsync("", "val"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => storage.SetAsync("key", null!));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.GetAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.RemoveAsync(""));
    }

    [Fact]
    public async Task WebPlatformNotificationServiceShouldCompleteWithoutError()
    {
        var service = new WebPlatformNotificationService();

        var exception = await Record.ExceptionAsync(() =>
            service.ShowInAppNotificationAsync("Title", "Safe message without PHI", "Info"));

        Assert.Null(exception);
    }
}
