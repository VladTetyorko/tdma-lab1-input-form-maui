namespace Mobile_apps_L1_Model_view.Services;

// Sends the queued local changes to the server whenever the network is available (offline-first)
public interface ISyncService
{
    // Network available and not simulating offline
    bool IsOnline { get; }

    // Raised after every sync attempt, from any thread
    event EventHandler<SyncResult>? Completed;

    // Called once at app start: listens for network changes and tries a first sync
    void Start();

    // Fire and forget, used for triggers (app resumed): syncs when online, does nothing otherwise, never throws
    void RequestSync();

    // Pushes the queue in order and stops at the first failure. Returns what happened
    Task<SyncResult> SyncNowAsync();

    Task<int> GetPendingCountAsync();
}
