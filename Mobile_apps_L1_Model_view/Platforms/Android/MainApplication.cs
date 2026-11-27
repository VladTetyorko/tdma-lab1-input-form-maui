using Android.App;
using Android.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp()
    {
        var app = MauiProgram.CreateMauiApp();

        // Exceptions thrown on Android's own threads reach this event before the AppDomain one: same logger
        var handler = app.Services.GetRequiredService<UnhandledExceptionLogger>();
        AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
            handler.OnPlatformUnhandled(e.Exception, "AndroidEnvironment.UnhandledExceptionRaiser");
        return app;
    }
}