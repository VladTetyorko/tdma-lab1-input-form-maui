using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class ProductListViewModelTests
{
    private const string DrillBarcode = "4006381333931";
    private const string SawBarcode = "4820001000016";

    private readonly TestStore _store = new();
    private readonly ProductListViewModel _viewModel;

    public ProductListViewModelTests()
    {
        _viewModel = new ProductListViewModel(_store.Catalog, _store.CharacteristicService, _store.CurrentUser, _store.AppState, _store.Startup, _store.Navigation);
    }

    [Fact]
    public async Task Load_FillsItemsWithQuantityOnHand()
    {
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        var saw = await _store.AddProductAsync("Saw", SawBarcode);
        await _store.Operations.AddAsync(new Operation { ProductId = drill.Id, Direction = OperationDirection.In, Quantity = 10 });
        await _store.Operations.AddAsync(new Operation { ProductId = drill.Id, Direction = OperationDirection.Out, Quantity = 4 });

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, _viewModel.Products.Count);
        Assert.Contains(_viewModel.Products, p => p.Id == drill.Id && p.Name == "Drill" && p.Quantity == 6);
        Assert.Contains(_viewModel.Products, p => p.Id == saw.Id && p.Name == "Saw" && p.Quantity == 0);
    }

    [Fact]
    public async Task Load_ShowsCurrentUserNameAndRole()
    {
        await _store.SignInAsync(Role.Manager, "Olena Kovalenko");

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Olena Kovalenko (Manager)", _viewModel.CurrentUserName);
    }

    [Theory]
    [InlineData(Role.Manager, true)]
    [InlineData(Role.Storekeeper, false)]
    [InlineData(Role.Driver, false)]
    public async Task CanAddProduct_OnlyForManager(Role role, bool expected)
    {
        await _store.SignInAsync(role);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(expected, _viewModel.CanAddProduct);
        Assert.Equal(expected, _viewModel.AddProductCommand.CanExecute(null));
    }

    [Theory]
    [InlineData(Role.Manager, true)]
    [InlineData(Role.Storekeeper, false)]
    [InlineData(Role.Driver, false)]
    public async Task CanManageCharacteristics_OnlyForManager(Role role, bool expected)
    {
        await _store.SignInAsync(role);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(expected, _viewModel.CanManageCharacteristics);
        Assert.Equal(expected, _viewModel.OpenCharacteristicsCommand.CanExecute(null));
    }

    [Fact]
    public async Task OpenCharacteristics_NavigatesToCharacteristicsPage()
    {
        await _store.SignInAsync(Role.Manager);
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.OpenCharacteristicsCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Characteristics, Assert.Single(_store.Navigation.RequestedRoutes));
    }

    [Fact]
    public async Task AddProduct_NavigatesToProductForm()
    {
        await _store.SignInAsync(Role.Manager, "Olena Kovalenko");
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.AddProductCommand.ExecuteAsync(null);

        Assert.Equal(Routes.ProductForm, Assert.Single(_store.Navigation.RequestedRoutes));
    }

    [Fact]
    public async Task SignOut_SignsOutAndNavigatesToLogin()
    {
        await _store.SignInAsync(Role.Manager, "Olena Kovalenko");

        await _viewModel.SignOutCommand.ExecuteAsync(null);

        Assert.Null(_store.CurrentUser.CurrentUser);
        Assert.Equal(Routes.Absolute(Routes.Login), Assert.Single(_store.Navigation.RequestedRoutes));
    }

    [Fact]
    public async Task EnsureLoaded_LoadsOnceForTheSameUser()
    {
        await _store.SignInAsync(Role.Manager);
        await _viewModel.EnsureLoadedCommand.ExecuteAsync(null);
        await _store.AddProductAsync("Drill", DrillBarcode);

        await _viewModel.EnsureLoadedCommand.ExecuteAsync(null);

        Assert.Empty(_viewModel.Products);
    }

    [Fact]
    public async Task EnsureLoaded_ReloadsWhenAnotherUserSignsIn()
    {
        await _store.SignInAsync(Role.Manager);
        await _viewModel.EnsureLoadedCommand.ExecuteAsync(null);
        await _store.AddProductAsync("Drill", DrillBarcode);
        await _store.SignInAsync(Role.Storekeeper);

        await _viewModel.EnsureLoadedCommand.ExecuteAsync(null);

        Assert.Equal("Drill", Assert.Single(_viewModel.Products).Name);
        Assert.Equal("Test User (Storekeeper)", _viewModel.CurrentUserName);
    }

    [Fact]
    public async Task ApplyQueryAttributes_UpdatedProduct_ReplacesTheRowWithoutAddingOne()
    {
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        await _store.SignInAsync(Role.Manager);
        await _viewModel.EnsureLoadedCommand.ExecuteAsync(null);
        var updated = new ProductSummary(drill.Id, "Hammer Drill", DrillBarcode, 99);

        _viewModel.ApplyQueryAttributes(Query(NavigationKeys.UpdatedProduct, updated));

        var row = Assert.Single(_viewModel.Products);
        Assert.Equal("Hammer Drill", row.Name);
        Assert.Equal(99, row.Quantity);
    }

    [Fact]
    public async Task ApplyQueryAttributes_NewProduct_AddsARow()
    {
        await _store.SignInAsync(Role.Manager);
        await _viewModel.EnsureLoadedCommand.ExecuteAsync(null);
        var created = new ProductSummary(Guid.NewGuid(), "Saw", SawBarcode, 0);

        _viewModel.ApplyQueryAttributes(Query(NavigationKeys.UpdatedProduct, created));

        Assert.Same(created, Assert.Single(_viewModel.Products));
    }

    [Fact]
    public async Task ApplyQueryAttributes_UpdatedProduct_ResortsByTheCurrentOption()
    {
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        await _store.AddProductAsync("Saw", SawBarcode);
        await _store.SignInAsync(Role.Manager);
        await _viewModel.EnsureLoadedCommand.ExecuteAsync(null);
        Assert.Equal("Drill", _viewModel.Products[0].Name);

        _viewModel.ApplyQueryAttributes(Query(NavigationKeys.UpdatedProduct,
            new ProductSummary(drill.Id, "Zebra Drill", DrillBarcode, 0)));

        Assert.Equal(new[] { "Saw", "Zebra Drill" }, _viewModel.Products.Select(p => p.Name));
    }

    [Fact]
    public async Task ApplyQueryAttributes_WithoutUpdatedProduct_ChangesNothing()
    {
        await _store.AddProductAsync("Drill", DrillBarcode);
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.ApplyQueryAttributes(new Dictionary<string, object>());

        Assert.Single(_viewModel.Products);
    }

    [Fact]
    public async Task EnsureLoaded_AfterCharacteristicsChanged_AddsNewSortOptions()
    {
        await _store.SignInAsync(Role.Manager);
        await _viewModel.EnsureLoadedCommand.ExecuteAsync(null);
        Assert.Equal(new[] { "Name", "Quantity" }, _viewModel.SortOptions.Select(o => o.Title));
        await _store.AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number);

        _viewModel.ApplyQueryAttributes(Query(NavigationKeys.CharacteristicsChanged, true));
        await _viewModel.EnsureLoadedCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "Name", "Quantity", "Weight (kg)" }, _viewModel.SortOptions.Select(o => o.Title));
    }

    [Fact]
    public async Task EnsureLoaded_WithoutCharacteristicsChanged_DoesNotRebuildSortOptions()
    {
        await _store.SignInAsync(Role.Manager);
        await _viewModel.EnsureLoadedCommand.ExecuteAsync(null);
        await _store.AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number);

        await _viewModel.EnsureLoadedCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "Name", "Quantity" }, _viewModel.SortOptions.Select(o => o.Title));
    }

    [Fact]
    public async Task SelectedSortOption_Quantity_ReordersTheRows()
    {
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        var saw = await _store.AddProductAsync("Saw", SawBarcode);
        await _store.Operations.AddAsync(new Operation { ProductId = drill.Id, Direction = OperationDirection.In, Quantity = 5 });
        await _store.Operations.AddAsync(new Operation { ProductId = saw.Id, Direction = OperationDirection.In, Quantity = 1 });
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.SelectedSortOption = SortOption.ByQuantity;

        Assert.Equal(new[] { "Saw", "Drill" }, _viewModel.Products.Select(p => p.Name));
    }

    [Fact]
    public async Task SortDescending_ReversesTheOrder()
    {
        await _store.AddProductAsync("Drill", DrillBarcode);
        await _store.AddProductAsync("Saw", SawBarcode);
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.SortDescending = true;

        Assert.Equal(new[] { "Saw", "Drill" }, _viewModel.Products.Select(p => p.Name));
    }

    [Fact]
    public async Task SelectedSortOption_Characteristic_OrdersNumbersNumerically()
    {
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        var saw = await _store.AddProductAsync("Saw", SawBarcode);
        var shelf = await _store.AddCharacteristicAsync("Shelf", CharacteristicValueType.Number);
        await _store.SetValueAsync(drill, shelf, "10");
        await _store.SetValueAsync(saw, shelf, "9");
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.SelectedSortOption = _viewModel.SortOptions.Single(o => o.Title == "Shelf");

        Assert.Equal(new[] { "Saw", "Drill" }, _viewModel.Products.Select(p => p.Name));
    }

    [Fact]
    public async Task OpenDetails_NavigatesToDetailWithSelectedProductParameter()
    {
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        var row = _viewModel.Products.Single(p => p.Id == drill.Id);

        await _viewModel.OpenDetailsCommand.ExecuteAsync(row);

        Assert.Equal(Routes.ProductDetail, Assert.Single(_store.Navigation.RequestedRoutes));
        var parameters = Assert.Single(_store.Navigation.RequestedParameters)!;
        Assert.Same(row, Assert.IsType<ProductSummary>(parameters[NavigationKeys.SelectedProduct]));
    }

    [Fact]
    public async Task OpenDetails_NullProduct_DoesNothing()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.OpenDetailsCommand.ExecuteAsync(null);

        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    private static Dictionary<string, object> Query(string key, object value) => new() { [key] = value };
}
