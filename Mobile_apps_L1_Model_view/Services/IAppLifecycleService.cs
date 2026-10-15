namespace Mobile_apps_L1_Model_view.Services;

// Receives the window events from App.CreateWindow (which only forwards them) and keeps the
// app state in step with them
public interface IAppLifecycleService
{
    // Called once, before the first page exists: loads the saved state and, if a user was signed in,
    // signs them in again; applies the saved theme. Returns the route to start on (Routes.Products or Routes.Login)
    Task<string> RestoreAsync();

    void OnCreated();

    void OnActivated();

    void OnDeactivated();

    // Window.Stopped: the app is in the background and may be killed without further warning
    void OnStopped();

    void OnResumed();

    // Window.Destroying
    void OnDestroying();
}
