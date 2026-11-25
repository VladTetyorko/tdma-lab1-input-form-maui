using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class HomeViewModelTests
{
    private const string Drill = "4006381333931";

    private readonly TestStore _store = new();
    private readonly StockDocumentDraftStore _drafts;
    private readonly StockChanges _changes = new();

    public HomeViewModelTests()
    {
        _drafts = new StockDocumentDraftStore(_store.AppState, new AesFieldEncryptor(_store.Secrets),
            new CapturingLogger<StockDocumentDraftStore>());
    }

    private async Task<HomeViewModel> OpenAsync(Role role)
    {
        await _store.SignInAsync(role, "Petro Bondarenko");
        var recent = new RecentActivityViewModel(_store.Operations, _store.Products, _store.Users,
            new InMemoryStockDocumentRepository(_store.Operations), _store.CurrentUser, _store.Clock,
            _store.Logger<RecentActivityViewModel>());
        var viewModel = new HomeViewModel(_store.CurrentUser, _drafts, _store.Startup, _store.ShakeGesture,
            _store.Navigation, _store.Clock, _changes, recent, _store.Logger<HomeViewModel>());
        await viewModel.AppearCommand.ExecuteAsync(null);
        return viewModel;
    }

    private Task SaveDraftAsync(int lines)
    {
        var draft = new StockDocumentDraft(OperationDirection.In);
        for (var i = 0; i < lines; i++)
        {
            draft.Add(Ean13.WithCheckDigit($"48200000000{i}"));
        }

        return _drafts.SaveAsync(draft);
    }

    [Fact]
    public async Task Manager_SeesBothTilesAndAddProduct()
    {
        var viewModel = await OpenAsync(Role.Manager);

        Assert.True(viewModel is { CanReceive: true, CanIssue: true, CanAddProduct: true, HasIssueNote: false });
        Assert.EndsWith(", Petro", viewModel.Greeting);
        Assert.Equal("Manager", viewModel.RoleText);
    }

    [Fact]
    public async Task Driver_HasNoIssueTileAndIsToldWhy()
    {
        var viewModel = await OpenAsync(Role.Driver);

        Assert.True(viewModel is { CanReceive: true, CanIssue: false, CanAddProduct: false, HasIssueNote: true });
        Assert.Equal(HomeActions.IssueNotAllowed, viewModel.IssueNote);
    }

    [Fact]
    public async Task DraftCaption_AppearsAndDisappearsWithTheDraft()
    {
        var viewModel = await OpenAsync(Role.Storekeeper);
        Assert.Equal(HomeActions.ReceiveHint, viewModel.ReceiveCaption);

        await SaveDraftAsync(2);
        Assert.Equal("Draft: 2 lines · continue", viewModel.ReceiveCaption);
        Assert.Equal(HomeActions.IssueHint, viewModel.IssueCaption);

        _drafts.Clear(OperationDirection.In);
        Assert.Equal(HomeActions.ReceiveHint, viewModel.ReceiveCaption);
    }

    [Fact]
    public async Task Tiles_NavigateToReceiveAndIssue()
    {
        var viewModel = await OpenAsync(Role.Storekeeper);

        await viewModel.OpenReceiveCommand.ExecuteAsync(null);
        await viewModel.OpenIssueCommand.ExecuteAsync(null);
        await viewModel.FindProductCommand.ExecuteAsync(null);
        await viewModel.ScanCommand.ExecuteAsync(null);

        Assert.Equal([Routes.Receive, Routes.Issue, Routes.Products, Routes.Scanner], _store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task RefusedActions_DoNotNavigateAndAreLogged()
    {
        var viewModel = await OpenAsync(Role.Driver);

        await viewModel.OpenIssueCommand.ExecuteAsync(null);
        await viewModel.AddProductCommand.ExecuteAsync(null);

        Assert.Empty(_store.Navigation.RequestedRoutes);
        Assert.Equal(2, _store.Logs.Count(log => log.Level == LogLevel.Warning));
    }

    [Fact]
    public async Task FindProduct_MarksTheProductListForReloading()
    {
        var viewModel = await OpenAsync(Role.Manager);

        await viewModel.AddProductCommand.ExecuteAsync(null);
        Assert.Equal(0, _changes.Version);
        await viewModel.FindProductCommand.ExecuteAsync(null);

        Assert.Equal([Routes.ProductForm, Routes.Products], _store.Navigation.RequestedRoutes);
        Assert.Equal(1, _changes.Version);
    }

    [Fact]
    public async Task Appearing_HandlesAPendingDeepLink()
    {
        await _store.AddProductAsync("Drill", Drill);
        _store.DeepLinks.Defer(DeepLinkParser.ForProduct(Drill));

        await OpenAsync(Role.Storekeeper);

        Assert.Equal(Routes.ProductDetail, Assert.Single(_store.Navigation.RequestedRoutes));
    }

    [Fact]
    public async Task Shake_ListensOnlyWhileThePageIsShown()
    {
        var viewModel = await OpenAsync(Role.Storekeeper);
        Assert.True(_store.ShakeDetector.IsRunning);

        viewModel.OnDisappearing();

        Assert.False(_store.ShakeDetector.IsRunning);
    }
}
