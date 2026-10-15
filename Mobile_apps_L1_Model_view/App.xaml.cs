using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;
using Mobile_apps_L1_Model_view.Views;

namespace Mobile_apps_L1_Model_view;

// Wiring only: every window event is forwarded to IAppLifecycleService
public partial class App : Application
{
    private readonly IAppLifecycleService _lifecycle;

    public App(IAppLifecycleService lifecycle)
    {
        _lifecycle = lifecycle;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // The database is opened and the session restored asynchronously: a startup page is shown first
        // and replaced by the shell when the start route is known
        var window = new Window(new StartupPage(StartupStatus.Opening));
        window.Created += (_, _) => _lifecycle.OnCreated();
        window.Activated += (_, _) => _lifecycle.OnActivated();
        window.Deactivated += (_, _) => _lifecycle.OnDeactivated();
        window.Stopped += (_, _) => _lifecycle.OnStopped();
        window.Resumed += (_, _) => _lifecycle.OnResumed();
        window.Destroying += (_, _) => _lifecycle.OnDestroying();
        _ = ShowShellAsync(window);
        return window;
    }

    // Continues on the UI thread (the context captured by await), so assigning window.Page is safe
    private async Task ShowShellAsync(Window window)
    {
        try
        {
            var startRoute = await _lifecycle.RestoreAsync();
            window.Page = new AppShell(startRoute);
        }
        catch (Exception ex)
        {
            window.Page = new StartupPage(StartupStatus.Failed(ex.Message));
        }
    }
}
