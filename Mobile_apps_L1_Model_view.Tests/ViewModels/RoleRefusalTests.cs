using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

// A command that the UI hides or disables can still be invoked (here: ExecuteAsync ignores CanExecute).
// For every such command: the wrong role gets an alert with the reason and a Warning log, and no data changes
public class RoleRefusalTests
{
    private const string Barcode = "4006381333931";

    private readonly TestStore _store = new();

    private void AssertRefused(string expectedMessage)
    {
        var alert = Assert.Single(_store.Alerts.Shown);
        Assert.Equal(PermissionGuard.AlertTitle, alert.Title);
        Assert.Equal(expectedMessage, alert.Message);
        var warning = Assert.Single(_store.Logs, r => r.Level == LogLevel.Warning);
        Assert.Equal(nameof(PermissionGuard), warning.Category);
    }

    private ProductListViewModel CreateList() => new(
        _store.Catalog, _store.CharacteristicService, _store.ProductService, _store.CurrentUser, _store.Startup, _store.Alerts, _store.Guard, _store.Navigation, _store.ShakeGesture, NullLogger<ProductListViewModel>.Instance);

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task ProductList_Delete_AsNonManager_IsRefused_AndTheProductStays(Role role)
    {
        var product = await _store.AddProductAsync("Drill", Barcode);
        await _store.SignInAsync(role);
        var list = CreateList();
        await list.LoadCommand.ExecuteAsync(null);

        await list.DeleteProductCommand.ExecuteAsync(list.Products.Single());

        AssertRefused($"Your role ({role}) cannot delete products.");
        Assert.Equal($"Your role ({role}) cannot delete products.", list.StatusMessage);
        Assert.NotNull(await _store.Products.GetByIdAsync(product.Id));
        Assert.Empty(_store.Alerts.Confirmations);
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task ProductList_Edit_Add_AndOpenCharacteristics_AsNonManager_AreRefused_WithoutNavigating(Role role)
    {
        await _store.AddProductAsync("Drill", Barcode);
        await _store.SignInAsync(role);
        var list = CreateList();
        await list.LoadCommand.ExecuteAsync(null);

        await list.EditProductCommand.ExecuteAsync(list.Products.Single());
        await list.AddProductCommand.ExecuteAsync(null);
        await list.OpenCharacteristicsCommand.ExecuteAsync(null);

        Assert.Equal(3, _store.Alerts.Shown.Count);
        Assert.Equal(3, _store.Logs.Count(r => r.Level == LogLevel.Warning));
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task ProductList_Delete_AsManager_IsNotRefused()
    {
        await _store.AddProductAsync("Drill", Barcode);
        await _store.SignInAsync(Role.Manager);
        var list = CreateList();
        await list.LoadCommand.ExecuteAsync(null);

        await list.DeleteProductCommand.ExecuteAsync(list.Products.Single());

        Assert.Empty(_store.Alerts.Shown);
        Assert.Null(await _store.Products.GetByBarcodeAsync(Barcode));
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task ProductForm_Save_AsNonManager_IsRefused_AndNothingIsAdded(Role role)
    {
        await _store.SignInAsync(role);
        var form = _store.ProductForm;
        form.Name = "Drill";
        form.Barcode = Barcode;
        await form.SaveCommand.ExecuteAsync(null);

        AssertRefused($"Your role ({role}) cannot add products.");
        Assert.Equal($"Your role ({role}) cannot add products.", form.StatusMessage);
        Assert.Empty(await _store.Products.GetAllAsync());
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task Characteristics_Add_AsNonManager_IsRefused_AndNothingIsAdded(Role role)
    {
        await _store.SignInAsync(role);
        var viewModel = new CharacteristicsViewModel(_store.CharacteristicService, _store.Validator, _store.CurrentUser,
            _store.Guard, _store.Navigation) { NewName = "Color" };

        await viewModel.AddCommand.ExecuteAsync(null);

        AssertRefused($"Your role ({role}) cannot add characteristics.");
        Assert.Equal($"Your role ({role}) cannot add characteristics.", viewModel.StatusMessage);
        Assert.Empty(await _store.Characteristics.GetAllAsync());
    }

    [Fact]
    public async Task Operation_OutAsDriver_IsRefused_AndNoOperationIsRecorded()
    {
        var product = await _store.AddProductAsync("Drill", Barcode);
        await _store.SignInAsync(Role.Driver);
        var viewModel = new OperationViewModel(_store.Stock, _store.CurrentUser, _store.Guard, _store.Drafts, _store.Navigation,
            _store.Location, _store.Feedback, NullLogger<OperationViewModel>.Instance);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = new ProductSummary(product.Id, product.Name, product.Barcode, 0)
        });
        viewModel.SelectedDirection = OperationDirection.Out;
        viewModel.Quantity = 2;

        await viewModel.SaveCommand.ExecuteAsync(null);

        AssertRefused("Your role (Driver) cannot record Out operations.");
        Assert.Equal("Your role (Driver) cannot record Out operations.", viewModel.ErrorMessage);
        Assert.Empty(await _store.Operations.GetAllAsync());
    }

    [Fact]
    public async Task Operation_InAsDriver_IsAllowed()
    {
        var product = await _store.AddProductAsync("Drill", Barcode);
        await _store.SignInAsync(Role.Driver);
        var viewModel = new OperationViewModel(_store.Stock, _store.CurrentUser, _store.Guard, _store.Drafts, _store.Navigation,
            _store.Location, _store.Feedback, NullLogger<OperationViewModel>.Instance);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = new ProductSummary(product.Id, product.Name, product.Barcode, 0)
        });
        viewModel.SelectedDirection = OperationDirection.In;
        viewModel.Quantity = 2;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Empty(_store.Alerts.Shown);
        Assert.Single(await _store.Operations.GetAllAsync());
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task ProductDetail_SaveValues_AsNonManager_IsRefused_AndTheValueIsUnchanged(Role role)
    {
        var product = await _store.AddProductAsync("Drill", Barcode);
        var color = await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);
        await _store.SetValueAsync(product, color, "Red");
        await _store.SignInAsync(role);
        var viewModel = new ProductDetailViewModel(_store.Catalog, _store.CharacteristicService, _store.Validator,
            _store.CurrentUser, _store.Guard, _store.AppState, _store.Navigation, _store.ProductService, _store.PhotoEditor,
            _store.Maps, _store.Feedback, NullLogger<ProductDetailViewModel>.Instance,
            new StockDocumentDraftStore(_store.AppState, new AesFieldEncryptor(_store.Secrets), NullLogger<StockDocumentDraftStore>.Instance), _store.Clock);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = new ProductSummary(product.Id, product.Name, product.Barcode, 0)
        });
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.Characteristics.Single().Value = "Blue";

        await viewModel.SaveCommand.ExecuteAsync(null);

        AssertRefused($"Your role ({role}) cannot edit characteristic values.");
        Assert.Equal("Red", (await _store.Values.GetByProductAsync(product.Id)).Single().Value);
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task Scanner_CreateProduct_AsNonManager_IsRefused_WithoutNavigating(Role role)
    {
        await _store.SignInAsync(role);
        var viewModel = new ScannerViewModel(new FakeBarcodeScanner(), _store.Catalog, _store.CurrentUser, _store.Guard,
            _store.Navigation, new FakeProductLookup(), _store.ShakeGesture, NullLogger<ScannerViewModel>.Instance) { UnknownBarcode = Barcode };

        await viewModel.CreateProductCommand.ExecuteAsync(null);
        await viewModel.CreateFromLookupCommand.ExecuteAsync(null);

        Assert.Equal(2, _store.Alerts.Shown.Count);
        Assert.All(_store.Alerts.Shown, a => Assert.Equal($"Your role ({role}) cannot add products.", a.Message));
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task Command_WhenNobodyIsSignedIn_AsksToSignIn()
    {
        var viewModel = new CharacteristicsViewModel(_store.CharacteristicService, _store.Validator, _store.CurrentUser,
            _store.Guard, _store.Navigation) { NewName = "Color" };

        await viewModel.AddCommand.ExecuteAsync(null);

        AssertRefused("Sign in before you add characteristics.");
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task ServiceLayer_RefusesToo_WhenTheViewModelIsBypassed(Role role)
    {
        await _store.SignInAsync(role);

        var product = await _store.ProductService.AddAsync("Drill", Barcode);
        var characteristic = await _store.CharacteristicService.AddAsync("Color", CharacteristicValueType.Text);

        Assert.False(product.IsSuccess);
        Assert.False(characteristic.IsSuccess);
        Assert.Empty(await _store.Products.GetAllAsync());
        Assert.Empty(await _store.Characteristics.GetAllAsync());
    }
}
