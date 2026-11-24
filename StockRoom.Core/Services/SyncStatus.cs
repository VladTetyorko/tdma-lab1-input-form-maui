namespace Mobile_apps_L1_Model_view.Services;

public enum SyncStatus
{
    // Everything that was queued has been sent
    Synced,

    // No internet (or "Simulate offline" is on): nothing was sent, changes wait in the queue
    Offline,

    // A push failed: the queue was left as it is and will be retried
    Failed,

    // Another sync is already running
    Busy
}
