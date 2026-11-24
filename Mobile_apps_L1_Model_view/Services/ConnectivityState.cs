namespace Mobile_apps_L1_Model_view.Services;

// What the connectivity banner says
public enum ConnectivityState
{
    // No internet (or "Simulate offline" is on): changes are saved on the device and wait
    Offline,

    // A sync is pushing the queue
    Syncing,

    // Online, but some changes have not reached the server yet (the last sync failed or has not run)
    Pending,

    // Online and nothing waits
    Synced
}
