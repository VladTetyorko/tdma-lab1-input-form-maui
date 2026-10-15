namespace Mobile_apps_L1_Model_view.Services;

public sealed class MauiConnectivityMonitor : IConnectivityMonitor
{
    public MauiConnectivityMonitor()
    {
        Connectivity.Current.ConnectivityChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool HasInternet => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler? Changed;
}
