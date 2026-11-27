using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.EndToEnd;

// Offline -> a change is queued -> the network returns -> the real SyncService pushes it -> the banner follows
public class ConnectivityFlowTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public ConnectivityFlowTests()
    {
        var services = new ServiceCollection().AddStockRoomCore().AddFakePlatform();
        services.AddSingleton<INavigationService>(new FakeNavigationService());
        services.AddSingleton<IPreferencesStore>(new FakePreferencesStore());
        services.AddSingleton<IAlertService>(new FakeAlertService());
        _provider = services.BuildServiceProvider();
    }

    public void Dispose() => _provider.Dispose();

    private T Get<T>() where T : notnull => _provider.GetRequiredService<T>();

    [Fact]
    public async Task GoingOnline_TriggersASync_AndTheBannerShowsTheJourney()
    {
        await DemoAccounts.SignInAsync(_provider, Role.Storekeeper);
        var network = Get<FakeConnectivityMonitor>();
        var sync = Get<ISyncService>();
        var queue = Get<Mobile_apps_L1_Model_view.Data.ISyncQueue>();
        network.SetInternet(false);
        sync.Start();
        var banner = Get<ConnectivityBannerViewModel>();
        var seen = new List<ConnectivityState>();
        banner.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ConnectivityBannerViewModel.State))
            {
                seen.Add(banner.State);
            }
        };

        // The page appears while offline
        banner.Attach();
        await Eventually.WaitAsync(() => banner.State == ConnectivityState.Offline);

        // A change is recorded offline; the page appears again (back from the operation page) and counts the queue
        var product = (await Get<IProductCatalog>().GetSummaryByBarcodeAsync("3017620422003"))!;
        var result = await Get<IStockService>().RecordAsync(product.Id, OperationDirection.Out, 1);
        Assert.True(result.IsSuccess, result.Error);
        banner.Detach();
        banner.Attach();
        await Eventually.WaitAsync(() => banner.Text.StartsWith("Offline: ") && banner.Text.Contains("waiting"));
        Assert.True(await queue.CountAsync() > 0);

        // The network returns: no tap, no polling, the sync runs by itself
        network.SetInternet(true);
        await Eventually.WaitAsync(() => banner.State == ConnectivityState.Synced && banner.Text == "All changes synced");

        Assert.Equal(0, await queue.CountAsync());
        Assert.Contains(ConnectivityState.Synced, seen);
        Assert.True(Get<StubSyncApi>().Records.Count > 0);
    }

    [Fact]
    public async Task SyncNow_RaisesStartedThenCompleted_AndIsSyncingIsOnlyTrueInBetween()
    {
        var sync = Get<ISyncService>();
        var order = new List<string>();
        sync.Started += (_, _) => order.Add("started:" + sync.IsSyncing);
        sync.Completed += (_, _) => order.Add("completed:" + sync.IsSyncing);

        await sync.SyncNowAsync();

        Assert.Equal(["started:True", "completed:False"], order);
        Assert.False(sync.IsSyncing);
    }
}
