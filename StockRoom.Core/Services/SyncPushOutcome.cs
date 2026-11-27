namespace Mobile_apps_L1_Model_view.Services;

public enum SyncPushOutcome
{
    // The server stored the change
    Applied,

    // The server already has a newer version of this entity: last write wins, so the change was ignored
    Stale
}
