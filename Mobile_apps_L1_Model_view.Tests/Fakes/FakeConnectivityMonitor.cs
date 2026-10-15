using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class FakeConnectivityMonitor : IConnectivityMonitor
{
    public bool HasInternet { get; private set; } = true;

    public event EventHandler? Changed;

    // Changes the state and raises Changed, like the platform does
    public void SetInternet(bool hasInternet)
    {
        HasInternet = hasInternet;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
