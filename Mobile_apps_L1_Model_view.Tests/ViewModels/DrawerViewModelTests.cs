using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class DrawerViewModelTests
{
    private readonly TestStore _store = new();
    private readonly FakeNavigationDrawer _drawer = new();
    private readonly StockChanges _changes = new();
    private readonly DrawerViewModel _viewModel;

    public DrawerViewModelTests()
    {
        var status = new SessionStatusViewModel(_store.CurrentUser, _store.Session, _store.AppState, _store.Banner,
            _store.Navigation, _store.Logger<SessionStatusViewModel>());
        _viewModel = new DrawerViewModel(status, _store.CurrentUser, _store.Navigation, _drawer, _changes,
            _store.Logger<DrawerViewModel>());
    }

    private DrawerItem Item(string route) => _viewModel.Items.Single(item => item.Route == route);

    [Fact]
    public async Task Entries_FollowTheRoleOfWhoSignedIn()
    {
        await _store.SignInAsync(Role.Driver);
        Assert.DoesNotContain(_viewModel.Items, item => item.Route == Routes.Characteristics);

        await _store.SignInAsync(Role.Manager);

        Assert.Equal(NavigationMenu.DrawerFor(Role.Manager).Select(entry => entry.Route), _viewModel.Items.Select(item => item.Route));
        Assert.Equal("MenuItem_Map", Item(Routes.Map).AutomationId);
    }

    [Fact]
    public void Close_ClosesTheDrawer()
    {
        _drawer.Open();

        _viewModel.CloseCommand.Execute(null);

        Assert.False(_drawer.IsOpen);
    }

    [Fact]
    public async Task Open_ClosesTheDrawerAndNavigates()
    {
        await _store.SignInAsync(Role.Storekeeper);
        _drawer.Open();

        await _viewModel.OpenCommand.ExecuteAsync(Item(Routes.Map));

        Assert.False(_drawer.IsOpen);
        Assert.Equal(Routes.Map, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal(0, _changes.Version);
    }

    [Fact]
    public async Task Open_AnActionThatIsAlreadyOpen_DoesNotPushItAgain()
    {
        await _store.SignInAsync(Role.Storekeeper);
        _viewModel.Select(Routes.Map);

        await _viewModel.OpenCommand.ExecuteAsync(Item(Routes.Map));

        Assert.True(Item(Routes.Map).IsSelected);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task Open_AnEntryTheRoleDoesNotHave_IsRefused()
    {
        await _store.SignInAsync(Role.Manager);
        var characteristics = Item(Routes.Characteristics);
        await _store.SignInAsync(Role.Driver);

        await _viewModel.OpenCommand.ExecuteAsync(characteristics);

        Assert.Empty(_store.Navigation.RequestedRoutes);
        Assert.Contains(_store.Logs, log => log.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task SignOut_ClosesTheDrawerAndGoesToLogin()
    {
        await _store.SignInAsync(Role.Manager);
        _drawer.Open();

        await _viewModel.SignOutCommand.ExecuteAsync(null);

        Assert.False(_drawer.IsOpen);
        Assert.Null(_store.CurrentUser.CurrentUser);
        Assert.Equal(Routes.Absolute(Routes.Login), Assert.Single(_store.Navigation.RequestedRoutes));
    }
}
