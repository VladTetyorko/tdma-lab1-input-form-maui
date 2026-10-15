using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class ProductFormViewModelTests
{
    private const string ValidBarcode = "4006381333931";

    private readonly InMemoryProductRepository _products = new();
    private readonly CurrentUserService _currentUser = new();
    private readonly FakeNavigationService _navigation = new();
    private readonly ProductFormViewModel _viewModel;

    public ProductFormViewModelTests()
    {
        var validator = new ProductValidator(_products);
        var service = new ProductService(_products, validator, _currentUser, new FakeClock());
        _viewModel = new ProductFormViewModel(validator, service, _currentUser, new AppStateService(new FakePreferencesStore()), _navigation);
    }

    [Fact]
    public void Name_ShowsErrorWhenBlankAndClearsWhenFixed()
    {
        _viewModel.Name = "   ";
        Assert.Equal("Name is required.", _viewModel.NameError);

        _viewModel.Name = "Drill";
        Assert.Equal(string.Empty, _viewModel.NameError);
    }

    [Fact]
    public void Barcode_ShowsErrorForInvalidEan13()
    {
        _viewModel.Barcode = "4006381333932";

        Assert.Contains("valid EAN-13", _viewModel.BarcodeError);
    }

    [Fact]
    public void Barcode_ClearsErrorForValidEan13()
    {
        _viewModel.Barcode = "4006381333932";

        _viewModel.Barcode = ValidBarcode;

        Assert.Equal(string.Empty, _viewModel.BarcodeError);
    }

    [Fact]
    public void Save_IsDisabledWhenNotSignedIn()
    {
        FillValidForm();

        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public void Save_IsDisabledForNonManager(Role role)
    {
        _currentUser.SignIn(new User { FullName = "Test", Role = role });
        FillValidForm();

        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void Save_IsDisabledForInvalidInput()
    {
        _currentUser.SignIn(new User { FullName = "Olena Kovalenko", Role = Role.Manager });
        _viewModel.Name = "Drill";
        _viewModel.Barcode = "123";

        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void Save_IsEnabledForManagerWithValidInput()
    {
        _currentUser.SignIn(new User { FullName = "Olena Kovalenko", Role = Role.Manager });
        FillValidForm();

        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Save_DuplicateBarcode_SetsBarcodeErrorAndDoesNotNavigate()
    {
        _currentUser.SignIn(new User { FullName = "Olena Kovalenko", Role = Role.Manager });
        await _products.AddAsync(new Product { Name = "Existing", Barcode = ValidBarcode });
        FillValidForm();

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("A product with this barcode already exists.", _viewModel.BarcodeError);
        Assert.Empty(_navigation.RequestedRoutes);
        Assert.Single(await _products.GetAllAsync());
    }

    [Fact]
    public async Task Save_Success_AddsProductAndNavigatesBack()
    {
        _currentUser.SignIn(new User { FullName = "Olena Kovalenko", Role = Role.Manager });
        FillValidForm();

        await _viewModel.SaveCommand.ExecuteAsync(null);

        var stored = Assert.Single(await _products.GetAllAsync());
        Assert.Equal("Cordless Saw", stored.Name);
        Assert.Equal(ValidBarcode, stored.Barcode);
        Assert.Equal(Routes.Back, Assert.Single(_navigation.RequestedRoutes));
    }

    [Fact]
    public async Task Cancel_NavigatesBackWithoutSaving()
    {
        FillValidForm();

        await _viewModel.CancelCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Back, Assert.Single(_navigation.RequestedRoutes));
        Assert.Empty(await _products.GetAllAsync());
    }

    [Fact]
    public async Task Save_Success_SendsTheNewProductWithZeroQuantityBackToTheList()
    {
        _currentUser.SignIn(new User { FullName = "Olena Kovalenko", Role = Role.Manager });
        FillValidForm();

        await _viewModel.SaveCommand.ExecuteAsync(null);

        var stored = Assert.Single(await _products.GetAllAsync());
        Assert.Equal(Routes.Back, _navigation.RequestedRoutes.Last());
        var parameters = _navigation.RequestedParameters.Last()!;
        var created = Assert.IsType<ProductSummary>(parameters[NavigationKeys.UpdatedProduct]);
        Assert.Equal(stored.Id, created.Id);
        Assert.Equal("Cordless Saw", created.Name);
        Assert.Equal(ValidBarcode, created.Barcode);
        Assert.Equal(0, created.Quantity);
    }

    private void FillValidForm()
    {
        _viewModel.Name = "Cordless Saw";
        _viewModel.Barcode = ValidBarcode;
    }
}
