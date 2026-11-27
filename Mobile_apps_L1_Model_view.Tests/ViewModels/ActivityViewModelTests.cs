using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class ActivityViewModelTests
{
    private readonly TestStore _store = new();

    private ActivityViewModel Create() =>
        new(_store.Feed, _store.CurrentUser, _store.Viewer, _store.Clock, _store.Logger<ActivityViewModel>());

    private async Task<(Product Drill, Product Saw, User Petro, User Ivan)> SeedAsync()
    {
        _store.Clock.UtcNow = DateTime.SpecifyKind(new DateTime(2026, 11, 25, 12, 0, 0), DateTimeKind.Local).ToUniversalTime();
        var drill = await _store.AddProductAsync("Drill", "4006381333931");
        var saw = await _store.AddProductAsync("Saw", "4820001000016");
        var ivan = await _store.SignInAsync(Role.Driver, "Ivan Shevchenko");
        var petro = await _store.SignInAsync(Role.Storekeeper, "Petro Bondarenko");
        await RecordAsync(drill, 24, OperationDirection.In, ivan.Id, _store.Clock.UtcNow.AddDays(-1));
        await RecordAsync(drill, 3, OperationDirection.Out, petro.Id, _store.Clock.UtcNow.AddHours(-2));
        await RecordAsync(saw, 5, OperationDirection.In, petro.Id, _store.Clock.UtcNow.AddHours(-1));
        return (drill, saw, petro, ivan);
    }

    private Task RecordAsync(Product product, int quantity, OperationDirection direction, Guid user, DateTime at) =>
        _store.Operations.AddAsync(new Operation
        {
            ProductId = product.Id,
            Quantity = quantity,
            Direction = direction,
            CreatedByUserId = user,
            CreatedAt = at,
            UpdatedAt = at
        });

    private static IEnumerable<int> Quantities(ActivityViewModel viewModel) =>
        viewModel.Days.SelectMany(day => day.Rows).Select(row => row.Entry.Quantity);

    [Fact]
    public async Task Rows_AreNewestFirstAndGroupedByDay()
    {
        await SeedAsync();
        var viewModel = Create();

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(["Today", "Yesterday"], viewModel.Days.Select(day => day.Header));
        Assert.Equal([5, 3, 24], Quantities(viewModel));
        Assert.True(viewModel is { ShowBy: true, CanFilterMine: true });
    }

    [Fact]
    public async Task Chips_FilterByDirectionAndOwner()
    {
        await SeedAsync();
        var viewModel = Create();

        await viewModel.ShowDirectionCommand.ExecuteAsync("In");
        Assert.Equal([5, 24], Quantities(viewModel));

        await viewModel.ToggleMineCommand.ExecuteAsync(null);
        Assert.Equal([5], Quantities(viewModel));

        await viewModel.ShowDirectionCommand.ExecuteAsync("All");
        Assert.Equal([5, 3], Quantities(viewModel));
    }

    [Fact]
    public async Task Driver_GetsOnlyHisOwn_AlsoWhenHeAsksForAll()
    {
        var (_, _, _, ivan) = await SeedAsync();
        _store.CurrentUser.SignOut();
        _store.CurrentUser.SignIn(ivan);
        var viewModel = Create();

        await viewModel.ShowDirectionCommand.ExecuteAsync("All");

        Assert.Equal([24], Quantities(viewModel));
        Assert.True(viewModel is { ShowBy: false, CanFilterMine: false });
        Assert.Equal([24], (await _store.Feed.GetAsync(new OperationFilter())).Select(e => e.Quantity));
        var products = await _store.Products.GetAllAsync();
        var history = (await _store.Catalog.GetDetailsAsync(products.Single(p => p.Name == "Drill").Id))!.History;
        Assert.Equal([24], history.Select(item => item.Quantity));
    }

    [Fact]
    public async Task AProductFilter_ComesWithTheNavigationAndCanBeCleared()
    {
        var (_, saw, _, _) = await SeedAsync();
        var viewModel = Create();
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = new ProductSummary(saw.Id, saw.Name, saw.Barcode, 5)
        });

        await viewModel.LoadCommand.ExecuteAsync(null);
        Assert.Equal([5], Quantities(viewModel));
        Assert.True(viewModel.HasProduct);

        await viewModel.ClearProductCommand.ExecuteAsync(null);
        Assert.Equal([5, 3, 24], Quantities(viewModel));
    }

    [Fact]
    public async Task ARow_OpensItsSheet()
    {
        await SeedAsync();
        var viewModel = Create();
        await viewModel.LoadCommand.ExecuteAsync(null);
        _store.Sheets.OnShow = FakeSheetService.DismissAsync;

        await viewModel.OpenCommand.ExecuteAsync(viewModel.Days[0].Rows[0]);

        var sheet = Assert.IsType<OperationSheetViewModel>(Assert.Single(_store.Sheets.Shown));
        Assert.Equal("+5 Saw", sheet.Title);
        Assert.StartsWith("Received by Petro Bondarenko · Today", sheet.ByText);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task ARow_SaysSignTimeAndInitials()
    {
        await SeedAsync();
        var viewModel = Create();
        await viewModel.LoadCommand.ExecuteAsync(null);

        var rows = viewModel.Days.SelectMany(day => day.Rows).ToList();

        Assert.Equal(("+5", "11:00", "PB", true), (rows[0].Amount, rows[0].Time, rows[0].Initials, rows[0].IsIn));
        Assert.Equal(("−3", true), (rows[1].Amount, rows[1].IsOut));
        Assert.Equal("Yesterday", (rows[2] with { ShowDay = true }).Time);
        Assert.Equal("1 Nov", OperationRowItem.Day(_store.Clock.UtcNow.AddDays(-24), _store.Clock.UtcNow));
    }
}
