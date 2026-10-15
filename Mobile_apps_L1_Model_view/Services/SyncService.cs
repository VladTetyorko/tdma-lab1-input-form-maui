using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;

namespace Mobile_apps_L1_Model_view.Services;

// Triggers: network change, "Simulate offline" switched, app resumed, and the Sync now button.
// Items are pushed oldest first. A pushed item is removed; a failed push stops the run and leaves the
// rest queued, so order is kept and the next trigger retries. Pushing is at-least-once: the server
// ignores an item it has already seen (same entity, same change time).
public sealed class SyncService(
    ISyncQueue queue,
    ISyncApi api,
    IConnectivityMonitor connectivity,
    IOfflineSimulator offline,
    IClock clock,
    ILogger<SyncService> logger) : ISyncService, IDisposable
{
    private readonly SemaphoreSlim _running = new(1, 1);
    private bool _started;
    private Task _lastRun = Task.CompletedTask;

    public bool IsOnline => connectivity.HasInternet && !offline.IsOffline;

    public event EventHandler<SyncResult>? Completed;

    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        connectivity.Changed += (_, _) => RequestSync();
        offline.Changed += (_, _) => RequestSync();
        RequestSync();
    }

    public void RequestSync()
    {
        if (IsOnline)
        {
            _lastRun = RunSafelyAsync();
        }
    }

    // Lets a sync that is running finish before the database is closed (the container disposes this first)
    public void Dispose() => _lastRun.Wait(TimeSpan.FromSeconds(5));

    public Task<int> GetPendingCountAsync() => queue.CountAsync();

    public async Task<SyncResult> SyncNowAsync()
    {
        if (!IsOnline)
        {
            return Publish(new SyncResult(SyncStatus.Offline, 0, await queue.CountAsync()));
        }

        if (!await _running.WaitAsync(0))
        {
            return new SyncResult(SyncStatus.Busy, 0, await queue.CountAsync());
        }

        try
        {
            return Publish(await PushAllAsync());
        }
        finally
        {
            _running.Release();
        }
    }

    private async Task<SyncResult> PushAllAsync()
    {
        var pushed = 0;
        foreach (var item in await queue.GetPendingAsync())
        {
            if (!IsOnline)
            {
                return new SyncResult(SyncStatus.Offline, pushed, await queue.CountAsync());
            }

            try
            {
                var outcome = await api.PushAsync(item);
                logger.LogInformation("Pushed {Operation} of {Type} {Id}: {Outcome}",
                    item.Operation, item.EntityType, item.EntityId, outcome);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Push of {Type} {Id} failed, it stays queued", item.EntityType, item.EntityId);
                await queue.MarkAttemptedAsync(item.Id, clock.UtcNow);
                return new SyncResult(SyncStatus.Failed, pushed, await queue.CountAsync(), ex.Message);
            }

            await queue.RemoveAsync(item.Id);
            pushed++;
        }

        return new SyncResult(SyncStatus.Synced, pushed, await queue.CountAsync());
    }

    private async Task RunSafelyAsync()
    {
        try
        {
            await SyncNowAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Synchronisation failed unexpectedly");
        }
    }

    private SyncResult Publish(SyncResult result)
    {
        Completed?.Invoke(this, result);
        return result;
    }
}
