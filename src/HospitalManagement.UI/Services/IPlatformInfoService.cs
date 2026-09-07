namespace HospitalManagement.UI.Services;

/// <summary>
/// Provides platform identification and device characteristics for host-agnostic UI.
/// </summary>
public interface IPlatformInfoService
{
    string PlatformName
    {
        get;
    }

    bool IsNative
    {
        get;
    }

    string DeviceIdiom
    {
        get;
    }

    string ApplicationVersion
    {
        get;
    }
}
