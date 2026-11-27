using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class BottomNavViewModelTests
{
    private const string Drill = "4006381333931";

    private readonly TestStore _store = new();
    private readonly StockChanges _changes = new();
    private readonly StockDocumentDraftStore _drafts;
    private readonly BottomNavViewModel _viewModel;

    public BottomNavViewModelTests()
    {
        _drafts = new StockDocumentDraftStore(_store.AppState, new AesFieldEncryptor(_store.Secrets),
            new CapturingLogger<StockDocumentDraftStore>());
        _viewModel = new BottomNavViewModel(_store.CurrentUser, _drafts, _store.Navigation, _changes,
            _store.Logger<BottomNavViewModel>());
    }

    [Fact]
    public void Selection_FollowsTheOpenRootPageOnly()
    {
        _viewModel.Select(Routes.Products);
        _viewModel.Select(Routes.ProductDetail);

        Assert.True(_viewModel.IsProducts);
        Assert.False(_viewModel.IsHome);
    }

    [Fact]
    public async Task Driver_HasNoIssue_AndCannotOpenIt()
    {
        await _store.SignInAsync(Role.Driver);

        await _viewModel.OpenCommand.ExecuteAsync(Routes.Issue);

        Assert.False(_viewModel.CanIssue);
        Assert.Empty(_store.Navigation.RequestedRoutes);
        Assert.Contains(_store.Logs, log => log.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Badges_FollowTheDrafts()
    {
        await _store.SignInAsync(Role.Manager);
        var draft = new StockDocumentDraft(OperationDirection.In);
        draft.Add(Drill, 4);

        await _drafts.SaveAsync(draft);

        Assert.Equal("1", _viewModel.ReceiveBadge);
        Assert.False(_viewModel.HasIssueBadge);
        _drafts.Clear(OperationDirection.In);
        Assert.False(_viewModel.HasReceiveBadge);
    }

    [Fact]
    public async Task Open_Products_NavigatesAndMakesTheListReadAgain()
    {
        await _store.SignInAsync(Role.Storekeeper);

        await _viewModel.OpenCommand.ExecuteAsync(Routes.Products);

        Assert.Equal(Routes.Products, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal(1, _changes.Version);
    }

    [Fact]
    public async Task Open_TheCurrentPlace_ReselectsInsteadOfNavigating()
    {
        await _store.SignInAsync(Role.Storekeeper);
        _store.Navigation.CurrentRoute = Routes.Search;
        var reselected = new List<string>();
        _viewModel.Reselected += (_, route) => reselected.Add(route);

        await _viewModel.OpenCommand.ExecuteAsync(Routes.Search);

        Assert.Equal([Routes.Search], reselected);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }
}
