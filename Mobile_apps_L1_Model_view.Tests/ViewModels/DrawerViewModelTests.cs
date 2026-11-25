using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class DrawerViewModelTests
{
    private const string Drill = "4006381333931";

    private readonly TestStore _store = new();
    private readonly FakeNavigationDrawer _drawer = new();
    private readonly StockChanges _changes = new();
    private readonly StockDocumentDraftStore _drafts;
    private readonly DrawerViewModel _viewModel;

    public DrawerViewModelTests()
    {
        _drafts = new StockDocumentDraftStore(_store.AppState, new AesFieldEncryptor(_store.Secrets),
            new CapturingLogger<StockDocumentDraftStore>());
        var status = new SessionStatusViewModel(_store.CurrentUser, _store.Session, _store.AppState, _store.Banner,
            _store.Navigation, _store.Logger<SessionStatusViewModel>());
        _viewModel = new DrawerViewModel(status, _store.CurrentUser, _drafts, _store.Navigation, _drawer, _changes,
            _store.Logger<DrawerViewModel>());
    }

    private DrawerItem Item(string route) => _viewModel.Items.Single(item => item.Route == route);

    [Fact]
    public async Task Entries_FollowTheRoleOfWhoSignedIn()
    {
        await _store.SignInAsync(Role.Driver);
        Assert.DoesNotContain(_viewModel.Items, item => item.Route is Routes.Issue or Routes.Characteristics);

        await _store.SignInAsync(Role.Manager);

        Assert.Equal(NavigationMenu.For(Role.Manager).Select(entry => entry.Route), _viewModel.Items.Select(item => item.Route));
        Assert.Equal("MenuItem_Receive", Item(Routes.Receive).AutomationId);
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

        await _viewModel.OpenCommand.ExecuteAsync(Item(Routes.Receive));

        Assert.False(_drawer.IsOpen);
        Assert.Equal(Routes.Receive, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal(0, _changes.Version);
    }

    [Fact]
    public async Task Open_Products_MakesTheListReadAgain()
    {
        await _store.SignInAsync(Role.Storekeeper);

        await _viewModel.OpenCommand.ExecuteAsync(Item(Routes.Products));

        Assert.Equal(1, _changes.Version);
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
        var issue = Item(Routes.Issue);
        await _store.SignInAsync(Role.Driver);

        await _viewModel.OpenCommand.ExecuteAsync(issue);

        Assert.Empty(_store.Navigation.RequestedRoutes);
        Assert.Contains(_store.Logs, log => log.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task DraftCounts_AreShownInTheTitles()
    {
        await _store.SignInAsync(Role.Manager);
        var draft = new StockDocumentDraft(OperationDirection.In);
        draft.Add(Drill, 4);

        await _drafts.SaveAsync(draft);

        Assert.Equal("Receive goods (1)", Item(Routes.Receive).Title);
        Assert.Equal("Issue goods", Item(Routes.Issue).Title);
        _drafts.Clear(OperationDirection.In);
        Assert.Equal("Receive goods", Item(Routes.Receive).Title);
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
