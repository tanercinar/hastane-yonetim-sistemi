using HospitalManagement.Maui.Services;
using HospitalManagement.UI.Services;

namespace HospitalManagement.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        builder.Services.AddLocalization();

        // Platform services DI registration
        builder.Services.AddSingleton<IPlatformInfoService, MauiPlatformInfoService>();
        builder.Services.AddSingleton<IPlatformConnectivityService, MauiPlatformConnectivityService>();
        builder.Services.AddSingleton<IAppSecureStorage, MauiPlatformSecureStorage>();
        builder.Services.AddSingleton<IPlatformNotificationService, MauiPlatformNotificationService>();
        builder.Services.AddSingleton<IPlatformBrowserService, MauiPlatformBrowserService>();

        return builder.Build();
    }
}
