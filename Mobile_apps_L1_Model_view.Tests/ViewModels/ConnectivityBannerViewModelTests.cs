using Moq;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class ConnectivityBannerViewModelTests
{
    private readonly TestStore _store = new();
    private readonly ConnectivityBannerViewModel _banner;

    public ConnectivityBannerViewModelTests()
    {
        _banner = _store.Banner;
    }

    private async Task AttachAsync()
    {
        _banner.Attach();
        await Eventually.WaitAsync(() => _banner.Text.Length > 0);
    }

    [Fact]
    public async Task Online_NothingWaiting_SaysAllSynced()
    {
        await AttachAsync();

        Assert.Equal(ConnectivityState.Synced, _banner.State);
        Assert.Equal("All changes synced", _banner.Text);
        Assert.False(_banner.IsOffline);
        Assert.True(_banner.IsNotOffline);
    }

    [Theory]
    [InlineData(1, "1 change waiting to sync")]
    [InlineData(3, "3 changes waiting to sync")]
    public async Task Online_WithQueuedChanges_SaysHowManyWait(int pending, string text)
    {
        _store.Sync.Pending = pending;

        await AttachAsync();

        Assert.Equal(ConnectivityState.Pending, _banner.State);
        Assert.Equal(text, _banner.Text);
    }

    [Fact]
    public async Task Offline_SaysOffline_WithTheNumberOfWaitingChanges()
    {
        _store.Sync.IsOnline = false;
        _store.Sync.Pending = 2;

        await AttachAsync();

        Assert.Equal(ConnectivityState.Offline, _banner.State);
        Assert.Equal("Offline: 2 changes waiting for a connection", _banner.Text);
        Assert.True(_banner.IsOffline);
    }

    [Fact]
    public async Task Offline_NothingWaiting_JustSaysOffline()
    {
        _store.Sync.IsOnline = false;

        await AttachAsync();

        Assert.Equal("Offline", _banner.Text);
    }

    [Fact]
    public async Task SyncStarted_ShowsSyncing_ThenCompletedShowsTheOutcome()
    {
        _store.Sync.Pending = 2;
        await AttachAsync();

        _store.Sync.RaiseStarted();
        Assert.Equal(ConnectivityState.Syncing, _banner.State);
        Assert.Equal("Syncing...", _banner.Text);

        _store.Sync.RaiseCompleted(new SyncResult(SyncStatus.Synced, 2, 0));
        Assert.Equal(ConnectivityState.Synced, _banner.State);
        Assert.Equal("All changes synced", _banner.Text);
    }

    [Fact]
    public async Task SyncFailed_KeepsTheRemainingChangesVisible()
    {
        await AttachAsync();

        _store.Sync.RaiseCompleted(new SyncResult(SyncStatus.Failed, 1, 4, "timeout"));

        Assert.Equal(ConnectivityState.Pending, _banner.State);
        Assert.Equal("4 changes waiting to sync", _banner.Text);
    }

    [Fact]
    public async Task SyncCompletedWhileOffline_ShowsOffline()
    {
        await AttachAsync();
        _store.Sync.IsOnline = false;

        _store.Sync.RaiseCompleted(new SyncResult(SyncStatus.Offline, 0, 5));

        Assert.Equal(ConnectivityState.Offline, _banner.State);
        Assert.Equal("Offline: 5 changes waiting for a connection", _banner.Text);
    }

    [Fact]
    public async Task NetworkLost_ThenRegained_TheBannerFollows()
    {
        await AttachAsync();

        _store.Sync.IsOnline = false;
        _store.Connectivity.SetInternet(false);
        await Eventually.WaitAsync(() => _banner.State == ConnectivityState.Offline);

        _store.Sync.IsOnline = true;
        _store.Connectivity.SetInternet(true);
        await Eventually.WaitAsync(() => _banner.State == ConnectivityState.Synced);
    }

    [Fact]
    public async Task SimulateOfflineSwitch_UpdatesTheBanner()
    {
        await AttachAsync();

        _store.Sync.IsOnline = false;
        _store.Offline.IsOffline = true;

        await Eventually.WaitAsync(() => _banner.State == ConnectivityState.Offline);
    }

    [Fact]
    public async Task Detach_StopsListening()
    {
        await AttachAsync();
        _banner.Detach();

        _store.Sync.RaiseStarted();
        _store.Connectivity.SetInternet(false);
        _store.Offline.IsOffline = true;

        Assert.Equal(ConnectivityState.Synced, _banner.State);
        Assert.Equal("All changes synced", _banner.Text);
    }

    [Fact]
    public async Task AttachingTwice_ListensOnce_AndAttachingAgainAfterDetachRefreshes()
    {
        await AttachAsync();
        _banner.Attach();
        _store.Sync.Pending = 7;
        _banner.Detach();
        _banner.Attach();

        await Eventually.WaitAsync(() => _banner.Text == "7 changes waiting to sync");
    }

    [Fact]
    public async Task Refresh_QueueCannotBeRead_IsLoggedAndDoesNotThrow()
    {
        var sync = new Moq.Mock<ISyncService>();
        sync.SetupGet(s => s.IsOnline).Returns(true);
        sync.Setup(s => s.GetPendingCountAsync()).ThrowsAsync(new IOException("closed"));
        var logger = new CapturingLogger<ConnectivityBannerViewModel>();
        var banner = new ConnectivityBannerViewModel(sync.Object, Moq.Mock.Of<IOfflineSimulator>(),
            Moq.Mock.Of<IConnectivityMonitor>(), logger);

        await banner.RefreshCommand.ExecuteAsync(null);

        Assert.Contains(logger.Records, r => r.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
    }
}
