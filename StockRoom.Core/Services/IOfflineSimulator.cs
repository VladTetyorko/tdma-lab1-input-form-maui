namespace Mobile_apps_L1_Model_view.Services;

// Demo switch ("Simulate offline" in Settings): makes the app behave as if there were no network, even when
// there is one. Easier than airplane mode on Windows, and the same on every platform. Not saved between runs.
public interface IOfflineSimulator
{
    bool IsOffline { get; set; }

    event EventHandler? Changed;
}
