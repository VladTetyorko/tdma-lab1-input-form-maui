using Mobile_apps_L1_Model_view.Services;

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
        // Same pattern as the data seeding in MauiProgram: in-memory lookups complete synchronously.
        // Lab 4 (SQLite) revisits this
        var startRoute = _lifecycle.RestoreAsync().GetAwaiter().GetResult();

        var window = new Window(new AppShell(startRoute));
        window.Created += (_, _) => _lifecycle.OnCreated();
        window.Activated += (_, _) => _lifecycle.OnActivated();
        window.Deactivated += (_, _) => _lifecycle.OnDeactivated();
        window.Stopped += (_, _) => _lifecycle.OnStopped();
        window.Resumed += (_, _) => _lifecycle.OnResumed();
        window.Destroying += (_, _) => _lifecycle.OnDestroying();
        return window;
    }
}
