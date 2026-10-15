using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// A fake server that lives in memory. It applies "last write wins": a change is accepted only when it is
// not older than what the server already has for that entity (the change time is SyncQueueItem.CreatedAt).
public sealed class StubSyncApi : ISyncApi
{
    private readonly object _gate = new();
    private readonly Dictionary<(string Type, Guid Id), StubServerRecord> _records = [];

    // Makes every push fail, like a server that is down
    public bool Fail { get; set; }

    public IReadOnlyList<StubServerRecord> Records
    {
        get
        {
            lock (_gate)
            {
                return _records.Values.ToList();
            }
        }
    }

    public Task<SyncPushOutcome> PushAsync(SyncQueueItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (Fail)
        {
            throw new SyncApiException("The server is not available (simulated failure).");
        }

        lock (_gate)
        {
            var key = (item.EntityType, item.EntityId);
            if (_records.TryGetValue(key, out var current) && current.ChangedAt > item.CreatedAt)
            {
                return Task.FromResult(SyncPushOutcome.Stale);
            }

            _records[key] = new StubServerRecord(
                item.EntityType, item.EntityId, item.Operation, item.PayloadJson, item.CreatedAt);
            return Task.FromResult(SyncPushOutcome.Applied);
        }
    }
}
