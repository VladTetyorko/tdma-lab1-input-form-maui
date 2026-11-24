using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Mobile_apps_L1_Model_view.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Mobile_apps_L1_Model_view.WinUI;

/// <summary>
///     Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	///     Initializes the singleton application object.  This is the first line of authored code
	///     executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
    {
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp()
    {
        var app = MauiProgram.CreateMauiApp();

        // Exceptions from the WinUI UI thread: same logger as the AppDomain and Task handlers
        var handler = app.Services.GetRequiredService<UnhandledExceptionLogger>();
        UnhandledException += (_, e) =>
            handler.OnPlatformUnhandled(e.Exception, "Microsoft.UI.Xaml.Application.UnhandledException");
        return app;
    }

    // The unpackaged app has no protocol activation, so a link arrives as a command-line argument:
    //   Mobile_apps_L1_Model_view.exe stockroom://product/3017620422003
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        var link = Environment.GetCommandLineArgs()
            .FirstOrDefault(a => a.StartsWith(DeepLinkParser.Scheme + ":", StringComparison.OrdinalIgnoreCase));
        if (link is not null)
        {
            IPlatformApplication.Current?.Services.GetService<IDeepLinkHandler>()?.Defer(link);
        }
    }
}
