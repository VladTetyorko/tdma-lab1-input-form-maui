using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

// The real queue (temporary SQLite file) and the stub server; only the network is faked
public class SyncServiceTests : IDisposable
{
    private readonly ServiceProvider _provider = new ServiceCollection().AddStockRoomCore().AddFakePlatform()
        .BuildServiceProvider();

    private ISyncService Sync => _provider.GetRequiredService<ISyncService>();
    private ISyncQueue Queue => _provider.GetRequiredService<ISyncQueue>();
    private StubSyncApi Server => _provider.GetRequiredService<StubSyncApi>();
    private FakeConnectivityMonitor Network => _provider.GetRequiredService<FakeConnectivityMonitor>();

    public void Dispose() => _provider.Dispose();

    private async Task<Product> AddProductAsync(string name = "Saw", string barcode = "4006381333931")
    {
        var product = new Product { Name = name, Barcode = barcode, CreatedByUserId = SeedData.ManagerId };
        await _provider.GetRequiredService<IProductRepository>().AddAsync(product);
        return product;
    }

    [Fact]
    public async Task SyncNow_Online_SendsTheQueueAndEmptiesIt()
    {
        var product = await AddProductAsync();

        var result = await Sync.SyncNowAsync();

        Assert.Equal(new SyncResult(SyncStatus.Synced, 1, 0), result);
        Assert.Equal(0, await Queue.CountAsync());
        var record = Assert.Single(Server.Records);
        Assert.Equal(("Products", product.Id), (record.EntityType, record.EntityId));
    }

    [Fact]
    public async Task SyncNow_NoInternet_KeepsTheChangesQueued()
    {
        Network.SetInternet(false);
        await AddProductAsync();

        var result = await Sync.SyncNowAsync();

        Assert.Equal(SyncStatus.Offline, result.Status);
        Assert.Equal(1, result.Remaining);
        Assert.Empty(Server.Records);
    }

    [Fact]
    public async Task SyncNow_SimulatedOffline_KeepsTheChangesQueued()
    {
        _provider.GetRequiredService<IOfflineSimulator>().IsOffline = true;
        await AddProductAsync();

        var result = await Sync.SyncNowAsync();

        Assert.Equal(SyncStatus.Offline, result.Status);
        Assert.False(Sync.IsOnline);
        Assert.Equal(1, await Queue.CountAsync());
    }

    [Fact]
    public async Task SyncNow_ServerFails_KeepsTheItemAndRetriesLater()
    {
        await AddProductAsync();
        Server.Fail = true;

        var failed = await Sync.SyncNowAsync();

        Assert.Equal(SyncStatus.Failed, failed.Status);
        Assert.Equal(1, failed.Remaining);
        Assert.NotNull(failed.Error);

        Server.Fail = false;
        var retried = await Sync.SyncNowAsync();
        Assert.Equal(new SyncResult(SyncStatus.Synced, 1, 0), retried);
    }

    [Fact]
    public async Task SyncNow_StopsAtTheFirstFailure_AndKeepsTheOrder()
    {
        var first = await AddProductAsync("Saw", "4006381333931");
        await AddProductAsync("Drill", "4820001999990");
        Server.Fail = true;

        await Sync.SyncNowAsync();

        var pending = await Queue.GetPendingAsync();
        Assert.Equal(2, pending.Count);
        Assert.Equal(first.Id, pending[0].EntityId);
    }

    [Fact]
    public async Task Start_ThenConnectionReturns_SendsWithoutAskingAgain()
    {
        Network.SetInternet(false);
        await AddProductAsync();
        Sync.Start();

        Network.SetInternet(true);

        await Eventually.WaitAsync(() => Server.Records.Count == 1);
        await Eventually.WaitAsync(() => Queue.CountAsync().GetAwaiter().GetResult() == 0);
    }

    [Fact]
    public async Task SyncNow_RaisesCompleted()
    {
        SyncResult? seen = null;
        Sync.Completed += (_, result) => seen = result;

        await Sync.SyncNowAsync();

        Assert.NotNull(seen);
    }

    [Fact]
    public async Task Server_KeepsTheNewerVersion_WhenAnOlderChangeArrivesLate()
    {
        var id = Guid.NewGuid();
        var newer = new SyncQueueItem { EntityType = "Products", EntityId = id, Operation = SyncOperation.Update, PayloadJson = "new", CreatedAt = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc) };
        var older = new SyncQueueItem { EntityType = "Products", EntityId = id, Operation = SyncOperation.Update, PayloadJson = "old", CreatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc) };

        Assert.Equal(SyncPushOutcome.Applied, await Server.PushAsync(newer));
        Assert.Equal(SyncPushOutcome.Stale, await Server.PushAsync(older));

        Assert.Equal("new", Assert.Single(Server.Records).PayloadJson);
    }

    [Fact]
    public async Task Server_DeleteIsKeptSoAnOlderUpdateCannotBringTheEntityBack()
    {
        var id = Guid.NewGuid();
        var delete = new SyncQueueItem { EntityType = "Products", EntityId = id, Operation = SyncOperation.Delete, CreatedAt = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc) };
        var update = new SyncQueueItem { EntityType = "Products", EntityId = id, Operation = SyncOperation.Update, CreatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc) };

        await Server.PushAsync(delete);
        await Server.PushAsync(update);

        Assert.True(Assert.Single(Server.Records).IsDeleted);
    }

    [Fact]
    public async Task Server_AcceptsTheSameItemTwice()
    {
        var item = new SyncQueueItem { EntityType = "Products", EntityId = Guid.NewGuid(), Operation = SyncOperation.Insert };

        Assert.Equal(SyncPushOutcome.Applied, await Server.PushAsync(item));
        Assert.Equal(SyncPushOutcome.Applied, await Server.PushAsync(item));
    }

    [Fact]
    public async Task SyncNow_WhileAnotherSyncIsRunning_ReturnsBusy_AndDoesNotPushTwice()
    {
        await AddProductAsync();
        var api = new GatedApi();
        var sync = new SyncService(Queue, api, Network, _provider.GetRequiredService<IOfflineSimulator>(),
            _provider.GetRequiredService<IClock>(), NullLogger<SyncService>.Instance);

        var first = sync.SyncNowAsync();
        await Eventually.WaitAsync(() => api.Pushes == 1);

        var second = await sync.SyncNowAsync();
        Assert.Equal(SyncStatus.Busy, second.Status);
        Assert.Equal(1, await Queue.CountAsync());

        api.Release();
        var finished = await first;
        Assert.Equal(new SyncResult(SyncStatus.Synced, 1, 0), finished);
        Assert.Equal(1, api.Pushes);
    }

    [Fact]
    public async Task SimulateOfflineSwitchedOff_SendsTheQueueWithoutAskingAgain()
    {
        var offline = _provider.GetRequiredService<IOfflineSimulator>();
        var sync = Sync;
        sync.Start();
        offline.IsOffline = true;
        await AddProductAsync();
        Assert.Empty(Server.Records);

        offline.IsOffline = false;

        await Eventually.WaitAsync(() => Server.Records.Count == 1);
        await Eventually.WaitAsync(() => Queue.CountAsync().GetAwaiter().GetResult() == 0);
    }

    [Fact]
    public async Task GetPendingCount_CountsEveryQueuedChange()
    {
        await AddProductAsync("Saw", "4006381333931");
        await AddProductAsync("Drill", "4006381333948");

        Assert.Equal(2, await Sync.GetPendingCountAsync());
    }

    // Blocks inside the first push until the test releases it, so a second sync can start meanwhile
    private sealed class GatedApi : ISyncApi
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _pushes;

        public int Pushes => Volatile.Read(ref _pushes);

        public void Release() => _gate.TrySetResult();

        public async Task<SyncPushOutcome> PushAsync(SyncQueueItem item, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _pushes);
            await _gate.Task;
            return SyncPushOutcome.Applied;
        }
    }
}
