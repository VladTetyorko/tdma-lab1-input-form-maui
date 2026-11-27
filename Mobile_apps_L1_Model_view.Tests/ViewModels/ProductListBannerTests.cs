using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

// The connectivity banner on the product list listens only while the page is visible
public class ProductListBannerTests
{
    private readonly TestStore _store = new();

    private ProductListViewModel CreateList() => new(
        _store.Catalog, _store.CharacteristicService, _store.ProductService, _store.CurrentUser, _store.Session,
        _store.AppState, _store.Startup, _store.Alerts, _store.Guard, _store.Navigation,
        _store.ShakeGesture, _store.Banner, _store.Logger<ProductListViewModel>());

    [Fact]
    public async Task Appearing_ShowsTheStateOfTheQueue()
    {
        await _store.SignInAsync(Role.Storekeeper);
        _store.Sync.Pending = 2;
        var list = CreateList();

        list.OnAppearing();

        await Eventually.WaitAsync(() => list.Connectivity.Text == "2 changes waiting to sync");
    }

    [Fact]
    public async Task Disappearing_StopsListeningToSyncEvents()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var list = CreateList();
        list.OnAppearing();
        await Eventually.WaitAsync(() => list.Connectivity.Text.Length > 0);

        list.OnDisappearing();
        _store.Sync.RaiseStarted();

        Assert.Equal(ConnectivityState.Synced, list.Connectivity.State);
    }

    [Fact]
    public async Task WhileVisible_SyncEventsReachTheBanner()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var list = CreateList();
        list.OnAppearing();
        await Eventually.WaitAsync(() => list.Connectivity.Text.Length > 0);

        _store.Sync.RaiseStarted();

        Assert.Equal("Syncing...", list.Connectivity.Text);
    }
}
