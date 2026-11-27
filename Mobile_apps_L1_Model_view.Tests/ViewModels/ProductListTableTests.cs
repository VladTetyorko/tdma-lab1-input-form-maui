using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

// Sorting by the column headers of the product table
public class ProductListTableTests
{
    private const string DrillBarcode = "4006381333931";
    private const string SawBarcode = "4820001000016";

    private readonly TestStore _store = new();
    private readonly ProductListViewModel _viewModel;

    public ProductListTableTests()
    {
        _viewModel = new ProductListViewModel(_store.Catalog, _store.CharacteristicService, _store.ProductService,
            _store.CurrentUser, _store.Startup, _store.Alerts, _store.Guard, _store.Navigation, _store.ShakeGesture,
            _store.Logger<ProductListViewModel>());
    }

    private async Task LoadTwoAsync()
    {
        await _store.SignInAsync(Role.Manager);
        await _store.AddProductAsync("Saw", SawBarcode);
        await _store.AddProductAsync("Drill", DrillBarcode);
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private IEnumerable<string> Names => _viewModel.Rows.Select(row => row.Name);

    [Fact]
    public async Task Load_SortsByNameAscendingAndShowsTheArrowOnName()
    {
        await LoadTwoAsync();

        Assert.Equal(["Drill", "Saw"], Names);
        Assert.True(_viewModel.NameColumn.IsAscending);
        Assert.DoesNotContain(_viewModel.Columns, column => column.IsSorted);
    }

    [Fact]
    public async Task SortByColumn_SameHeaderTwice_ReversesTheRows()
    {
        await LoadTwoAsync();

        _viewModel.SortByColumnCommand.Execute(_viewModel.NameColumn);

        Assert.Equal(["Saw", "Drill"], Names);
        Assert.True(_viewModel.NameColumn.IsSortedDescending);
    }

    [Fact]
    public async Task SortByColumn_AnotherHeader_StartsAscending()
    {
        await LoadTwoAsync();
        _viewModel.SortByColumnCommand.Execute(_viewModel.NameColumn);
        var quantity = _viewModel.Columns.Single(column => column.Sort == SortOption.ByQuantity);

        _viewModel.SortByColumnCommand.Execute(quantity);

        Assert.True(_viewModel.Columns.Single(column => column.IsSorted).IsAscending);
        Assert.False(_viewModel.NameColumn.IsSorted);
        Assert.Equal(["Drill", "Saw"], Names);
    }

    [Fact]
    public async Task SortByColumn_Barcode_DoesNothing()
    {
        await LoadTwoAsync();

        _viewModel.SortByColumnCommand.Execute(_viewModel.Columns.Single(column => column.Sort is null));

        Assert.True(_viewModel.NameColumn.IsAscending);
    }

    [Fact]
    public async Task Search_KeepsTheChosenOrder()
    {
        await LoadTwoAsync();
        _viewModel.SortByColumnCommand.Execute(_viewModel.NameColumn);

        _viewModel.SearchText = "sa";

        Assert.Equal(["Saw"], Names);
        Assert.True(_viewModel.NameColumn.IsSortedDescending);
    }

    [Fact]
    public async Task Columns_AreAlwaysBarcodeQuantityAndEveryCharacteristic()
    {
        await _store.AddCharacteristicAsync("Brand", CharacteristicValueType.Text);
        await _store.AddCharacteristicAsync("Weight", CharacteristicValueType.Number);
        await LoadTwoAsync();

        Assert.Equal(["Barcode", "Qty", "Brand", "Weight"], _viewModel.Columns.Select(column => column.Title));
        Assert.All(_viewModel.Rows, row => Assert.Equal(4, row.Cells.Count));
    }

    [Fact]
    public async Task TableMinimumWidth_FollowsCharacteristicsAndTheRoleOfTheUser()
    {
        await LoadTwoAsync();
        var manager = _viewModel.TableMinimumWidth;
        Assert.True(_viewModel.CanManageProducts);

        await _store.AddCharacteristicAsync("Colour", CharacteristicValueType.Text);
        _viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationKeys.CharacteristicsChanged] = true });
        await _viewModel.EnsureLoadedCommand.ExecuteAsync(null);
        Assert.Equal(manager + 110 + 8, _viewModel.TableMinimumWidth);

        _viewModel.CanManageProducts = false;
        Assert.Equal(manager + 118 - 112, _viewModel.TableMinimumWidth);
    }

    [Fact]
    public async Task CharacteristicAddedWhileRunning_AppearsAsAColumn()
    {
        await LoadTwoAsync();
        await _store.AddCharacteristicAsync("Colour", CharacteristicValueType.Text);
        _viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationKeys.CharacteristicsChanged] = true });

        await _viewModel.EnsureLoadedCommand.ExecuteAsync(null);

        Assert.Contains(_viewModel.Columns, column => column.Title == "Colour");
    }
}
