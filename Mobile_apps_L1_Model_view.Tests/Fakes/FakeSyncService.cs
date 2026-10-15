using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Records the calls; SyncNowAsync returns Result and Pending is what the queue "holds"
public sealed class FakeSyncService : ISyncService
{
    public bool IsOnline { get; set; } = true;

    public int StartCalls { get; private set; }

    public int RequestCalls { get; private set; }

    public int SyncNowCalls { get; private set; }

    public int Pending { get; set; }

    public SyncResult Result { get; set; } = new(SyncStatus.Synced, 0, 0);

    public event EventHandler<SyncResult>? Completed;

    public void Start() => StartCalls++;

    public void RequestSync() => RequestCalls++;

    public Task<SyncResult> SyncNowAsync()
    {
        SyncNowCalls++;
        Completed?.Invoke(this, Result);
        return Task.FromResult(Result);
    }

    public Task<int> GetPendingCountAsync() => Task.FromResult(Pending);
}
