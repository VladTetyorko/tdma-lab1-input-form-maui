namespace Mobile_apps_L1_Model_view.Services;

// Whether the device has internet access (platform abstraction over MAUI Connectivity)
public interface IConnectivityMonitor
{
    bool HasInternet { get; }

    // Raised when the network state changes (may come from any thread)
    event EventHandler? Changed;
}
