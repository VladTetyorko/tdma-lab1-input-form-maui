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
        _store.Clock.UtcNow = DateTime.SpecifyKind(new DateTime(2026, 11, 25, 12, 0, 0), DateTimeKind.Local).ToUniversalTime();
    }

    private async Task<HomeViewModel> OpenAsync(Role role)
    {
        if (_store.CurrentUser.CurrentUser?.Role != role)
        {
            await _store.SignInAsync(role, "Petro Bondarenko");
        }

        var viewModel = new HomeViewModel(_store.CurrentUser, _drafts, _store.Catalog, _store.Feed, _store.Viewer,
            _store.Startup, _store.ShakeGesture, _store.Navigation, _store.Clock, _changes, _store.Logger<HomeViewModel>());
        await viewModel.AppearCommand.ExecuteAsync(null);
        return viewModel;
    }

    private Task RecordAsync(Product product, int quantity, DateTime at, Guid user,
        OperationDirection direction = OperationDirection.In) =>
        _store.Operations.AddAsync(new Operation
        {
            ProductId = product.Id,
            Quantity = quantity,
            Direction = direction,
            CreatedByUserId = user,
            CreatedAt = at,
            UpdatedAt = at
        });

    private Task SaveDraftAsync(int lines)
    {
        var draft = new StockDocumentDraft(OperationDirection.In);
        for (var i = 0; i < lines; i++)
        {
            draft.Add(Ean13.WithCheckDigit($"48200000000{i}"), 3);
        }

        return _drafts.SaveAsync(draft);
    }

    [Fact]
    public async Task Greets_AndHasNothingToShowAtFirst()
    {
        var viewModel = await OpenAsync(Role.Manager);

        Assert.EndsWith(", Petro", viewModel.Greeting);
        Assert.False(viewModel.HasDrafts);
        Assert.False(viewModel.HasOutOfStock);
        Assert.False(viewModel.HasToday);
        Assert.Equal("Nothing recorded today.", viewModel.TodayEmptyText);
    }

    [Fact]
    public async Task ADraft_IsOfferedToContinue()
    {
        await SaveDraftAsync(2);
        var viewModel = await OpenAsync(Role.Storekeeper);

        var row = Assert.Single(viewModel.Drafts);
        Assert.Equal("Receive draft · 2 lines · 6 units", row.Text);
        await viewModel.ContinueCommand.ExecuteAsync(row);

        Assert.Equal(Routes.Receive, Assert.Single(_store.Navigation.RequestedRoutes));
    }

    [Fact]
    public async Task OutOfStock_ShowsAtMostThree_AndOpensTheTableSortedByQuantity()
    {
        foreach (var name in new[] { "D", "B", "C", "A" })
        {
            await _store.AddProductAsync(name, Ean13.WithCheckDigit($"48200000001{name[0] - 'A'}"));
        }

        var viewModel = await OpenAsync(Role.Storekeeper);

        Assert.Equal("Out of stock · 4", viewModel.OutOfStockTitle);
        Assert.Equal(["A", "B", "C"], viewModel.OutOfStock.Select(p => p.Name));
        await viewModel.OpenOutOfStockCommand.ExecuteAsync(null);
        Assert.Equal(Routes.Products, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.True(_store.Navigation.RequestedParameters[0]!.ContainsKey(NavigationKeys.SortByQuantity));
        Assert.Equal(1, _changes.Version);
    }

    [Fact]
    public async Task Today_ListsAtMostFiveOfTodayOnly()
    {
        var product = await _store.AddProductAsync("Drill", Drill);
        var user = await _store.SignInAsync(Role.Storekeeper, "Petro Bondarenko");
        await RecordAsync(product, 100, _store.Clock.UtcNow.AddDays(-1), user.Id);
        for (var i = 1; i <= 6; i++)
        {
            await RecordAsync(product, i, _store.Clock.UtcNow.AddMinutes(-10 + i), user.Id);
        }

        var viewModel = await OpenAsync(Role.Storekeeper);

        var rows = Assert.Single(viewModel.Today).Rows;
        Assert.Equal([6, 5, 4, 3, 2], rows.Select(r => r.Entry.Quantity));
        Assert.All(rows, row => Assert.True(row.ShowBy));
        Assert.Equal("Today", viewModel.TodayTitle);
    }

    [Fact]
    public async Task Driver_SeesOnlyHisOwnDeliveries_AndNoOutOfStockList()
    {
        var product = await _store.AddProductAsync("Drill", Drill);
        await _store.AddProductAsync("Saw", "4820001000016");
        var other = await _store.SignInAsync(Role.Storekeeper, "Petro Bondarenko");
        await RecordAsync(product, 7, _store.Clock.UtcNow, other.Id);
        var driver = await _store.SignInAsync(Role.Driver, "Ivan Shevchenko");
        await RecordAsync(product, 2, _store.Clock.UtcNow, driver.Id);

        var viewModel = await OpenAsync(Role.Driver);

        var row = Assert.Single(Assert.Single(viewModel.Today).Rows);
        Assert.Equal((2, false), (row.Entry.Quantity, row.ShowBy));
        Assert.Equal("My deliveries today", viewModel.TodayTitle);
        Assert.False(viewModel.HasOutOfStock);
    }

    [Fact]
    public async Task ARowOfTheTable_OpensTheOperationSheet_AndThenTheProduct()
    {
        var product = await _store.AddProductAsync("Drill", Drill);
        var user = await _store.SignInAsync(Role.Storekeeper, "Petro Bondarenko");
        await RecordAsync(product, 2, _store.Clock.UtcNow, user.Id);
        var viewModel = await OpenAsync(Role.Storekeeper);
        _store.Sheets.OnShow = sheet =>
        {
            sheet.SecondaryCommand.Execute(null);
            return Task.CompletedTask;
        };

        await viewModel.OpenOperationCommand.ExecuteAsync(viewModel.Today[0].Rows[0]);

        Assert.Equal(Routes.ProductDetail, Assert.Single(_store.Navigation.RequestedRoutes));
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
