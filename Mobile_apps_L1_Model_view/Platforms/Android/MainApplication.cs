using Android.App;
using Android.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        {
            // The last net for a camera that fails at run time (held by another app, hardware trouble): the pages
            // fall back to typing. Everything else stays unhandled
            if (CameraFailures.IsStartFailure(e.Exception.ToString()))
            {
                e.Handled = true;
                app.Services.GetRequiredService<ILogger<MainApplication>>()
                    .LogError(e.Exception, "The camera could not start");
                app.Services.GetRequiredService<IBarcodeScanner>().ReportStartFailure();
                return;
            }

            handler.OnPlatformUnhandled(e.Exception, "AndroidEnvironment.UnhandledExceptionRaiser");
        };
        return app;
    }
}