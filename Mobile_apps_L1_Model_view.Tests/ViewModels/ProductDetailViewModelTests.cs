using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class ProductDetailViewModelTests
{
    private const string DrillBarcode = "4006381333931";

    private readonly TestStore _store = new();
    private readonly ProductDetailViewModel _viewModel;

    public ProductDetailViewModelTests()
    {
        _viewModel = new ProductDetailViewModel(
            _store.Catalog, _store.CharacteristicService, _store.Validator, _store.CurrentUser, _store.AppState, _store.Navigation);
    }

    [Fact]
    public async Task ApplyQueryAttributes_ShowsTheSelectedProductBeforeLoad()
    {
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        await _store.Operations.AddAsync(new Operation { ProductId = drill.Id, Direction = OperationDirection.In, Quantity = 7 });

        await ApplyAsync(drill.Id);

        Assert.Equal("Drill", _viewModel.Name);
        Assert.Equal(DrillBarcode, _viewModel.Barcode);
        Assert.Equal(7, _viewModel.Quantity);
    }

    [Fact]
    public async Task Load_ShowsEveryCharacteristicWithEmptyValueWhenAbsent()
    {
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        var weight = await _store.AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number);
        var color = await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);
        await _store.SetValueAsync(drill, color, "Blue");

        await ApplyAsync(drill.Id);

        Assert.Equal(2, _viewModel.Characteristics.Count);
        Assert.Equal(string.Empty, _viewModel.Characteristics.Single(c => c.CharacteristicId == weight.Id).Value);
        Assert.Equal("Blue", _viewModel.Characteristics.Single(c => c.CharacteristicId == color.Id).Value);
    }

    [Fact]
    public async Task Load_FillsHistoryNewestFirst()
    {
        var manager = await _store.SignInAsync(Role.Manager);
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        await _store.Operations.AddAsync(new Operation
        {
            ProductId = drill.Id, Direction = OperationDirection.In, Quantity = 5, CreatedByUserId = manager.Id,
            CreatedAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc)
        });
        await _store.Operations.AddAsync(new Operation
        {
            ProductId = drill.Id, Direction = OperationDirection.Out, Quantity = 2, CreatedByUserId = manager.Id,
            CreatedAt = new DateTime(2026, 1, 2, 9, 0, 0, DateTimeKind.Utc)
        });

        await ApplyAsync(drill.Id);

        Assert.Equal(new[] { 2, 5 }, _viewModel.History.Select(h => h.Quantity));
    }

    [Fact]
    public async Task Load_ProductThatNoLongerExists_ShowsStatusMessage()
    {
        _viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = new ProductSummary(Guid.NewGuid(), "Gone", DrillBarcode, 0)
        });

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("This product no longer exists.", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task Load_ReadsQuantityAgainAfterAnOperationWasRecorded()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        await _store.Operations.AddAsync(new Operation { ProductId = drill.Id, Direction = OperationDirection.In, Quantity = 5 });
        await ApplyAsync(drill.Id);

        await _store.Stock.RecordAsync(drill.Id, OperationDirection.Out, 2);
        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(3, _viewModel.Quantity);
    }

    [Theory]
    [InlineData(Role.Manager, true)]
    [InlineData(Role.Storekeeper, false)]
    [InlineData(Role.Driver, false)]
    public async Task CanEdit_OnlyForManager(Role role, bool expected)
    {
        await _store.SignInAsync(role);
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);

        await ApplyAsync(drill.Id);

        Assert.Equal(expected, _viewModel.CanEdit);
        Assert.Equal(new[] { !expected }, _viewModel.Characteristics.Select(c => c.IsReadOnly).Distinct());
    }

    [Fact]
    public async Task CanEdit_FalseWhenNotSignedIn()
    {
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);

        await ApplyAsync(drill.Id);

        Assert.False(_viewModel.CanEdit);
    }

    [Fact]
    public async Task Save_IsDisabledWhenACharacteristicHasAnError()
    {
        await _store.SignInAsync(Role.Manager);
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        await _store.AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number);
        await ApplyAsync(drill.Id);
        var weight = _viewModel.Characteristics.Single();

        weight.Value = "heavy";
        Assert.False(_viewModel.SaveCommand.CanExecute(null));

        weight.Value = "2";
        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Save_IsDisabledForNonManager()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        await ApplyAsync(drill.Id);

        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Save_WithInvalidValue_StoresNothingAndDoesNotNavigate()
    {
        await _store.SignInAsync(Role.Manager);
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        var weight = await _store.AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number);
        await _store.SetValueAsync(drill, weight, "1.5");
        await ApplyAsync(drill.Id);
        _viewModel.Characteristics.Single().Value = "heavy";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Empty(_store.Navigation.RequestedRoutes);
        Assert.Equal("1.5", Assert.Single(await _store.Values.GetAllAsync()).Value);
    }

    [Fact]
    public async Task Save_Success_NavigatesBackWithTheFreshProduct()
    {
        await _store.SignInAsync(Role.Manager);
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        var weight = await _store.AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number);
        await ApplyAsync(drill.Id);
        _viewModel.Characteristics.Single().Value = "2.5";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Back, Assert.Single(_store.Navigation.RequestedRoutes));
        var updated = Assert.IsType<ProductSummary>(Assert.Single(_store.Navigation.RequestedParameters)![NavigationKeys.UpdatedProduct]);
        Assert.Equal(drill.Id, updated.Id);
        Assert.Equal("2.5", updated.Values[weight.Id]);
    }

    [Fact]
    public async Task Save_Failure_ShowsTheErrorAndStaysOnThePage()
    {
        await _store.SignInAsync(Role.Manager);
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);
        await ApplyAsync(drill.Id);
        _store.CurrentUser.SignOut();

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Sign in before you edit characteristic values.", _viewModel.StatusMessage);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task GoBack_SendsTheCurrentProductBackToTheList()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        await ApplyAsync(drill.Id);

        await _viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Back, Assert.Single(_store.Navigation.RequestedRoutes));
        var updated = Assert.IsType<ProductSummary>(Assert.Single(_store.Navigation.RequestedParameters)![NavigationKeys.UpdatedProduct]);
        Assert.Equal(drill.Id, updated.Id);
    }

    [Fact]
    public async Task GoBack_ProductNoLongerExists_SendsPlainBack()
    {
        _viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = new ProductSummary(Guid.NewGuid(), "Gone", DrillBarcode, 0)
        });

        await _viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Back, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Null(Assert.Single(_store.Navigation.RequestedParameters));
    }

    [Fact]
    public async Task OpenOperation_SendsTheSelectedProductToTheOperationPage()
    {
        var drill = await _store.AddProductAsync("Drill", DrillBarcode);
        await ApplyAsync(drill.Id);

        await _viewModel.OpenOperationCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Operation, Assert.Single(_store.Navigation.RequestedRoutes));
        var product = Assert.IsType<ProductSummary>(Assert.Single(_store.Navigation.RequestedParameters)![NavigationKeys.SelectedProduct]);
        Assert.Equal(drill.Id, product.Id);
    }

    [Fact]
    public async Task OpenOperation_WithoutAProduct_DoesNothing()
    {
        await _viewModel.OpenOperationCommand.ExecuteAsync(null);

        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    private async Task ApplyAsync(Guid productId)
    {
        var summary = await _store.Catalog.GetSummaryAsync(productId);
        _viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = summary!
        });
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
