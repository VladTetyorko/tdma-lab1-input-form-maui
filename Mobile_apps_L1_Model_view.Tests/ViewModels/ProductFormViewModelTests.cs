using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly FakeAlertService _alerts = new();
    private readonly AppStateService _appState = new(new FakePreferencesStore());
    private readonly FakePhotoService _photos = new();
    private readonly FakeConfirmSheet _confirm = new();
    private readonly FakeBarcodeScanner _scanner = new();
    private readonly FakeProductLookup _lookup = new();
    private readonly ProductFormViewModel _viewModel;

    public ProductFormViewModelTests()
    {
        _viewModel = CreateViewModel(_appState);
    }

    private ProductFormViewModel CreateViewModel(AppStateService appState)
    {
        var validator = new ProductValidator(_products);
        var service = new ProductService(_products, new InMemoryOperationRepository(), validator, _currentUser, new FakeClock(),
            NullLogger<ProductService>.Instance);
        var guard = new PermissionGuard(_currentUser, _alerts, NullLogger<PermissionGuard>.Instance);
        var photo = new ProductPhotoEditor(_photos, new DeviceFeedback(NullLogger<DeviceFeedback>.Instance), guard,
            NullLogger<ProductPhotoEditor>.Instance);
        var values = new InMemoryProductCharacteristicRepository();
        var operations = new InMemoryOperationRepository();
        var characteristics = new InMemoryCharacteristicRepository();
        var catalog = new ProductCatalog(_products, characteristics, values, operations, new InMemoryUserRepository(),
            new StockService(operations, _products, _currentUser, new FakeClock(), NullLogger<StockService>.Instance));
        var characteristicService = new CharacteristicService(characteristics, values, _products,
            new CharacteristicValidator(characteristics), _currentUser, new FakeClock(),
            NullLogger<CharacteristicService>.Instance);
        var search = new QuickSearchViewModel(catalog, characteristicService, _currentUser, guard, _navigation,
            new FakeKeyboard(),
            NullLogger<QuickSearchViewModel>.Instance);
        return new ProductFormViewModel(validator, service, _currentUser, guard, appState, _navigation, photo,
            _scanner, _lookup, search, _confirm, NullLogger<ProductFormViewModel>.Instance);
    }

    private async Task SignInAsManagerWithADrillAsync()
    {
        _currentUser.SignIn(new User { FullName = "Olena", Role = Role.Manager });
        await _products.AddAsync(new Product { Name = "Drill", Barcode = ValidBarcode });
        await _viewModel.AppearCommand.ExecuteAsync(null);
    }

    [Fact]
    public async Task Scan_FillsTheBarcodeAndStopsTheCamera()
    {
        _currentUser.SignIn(new User { FullName = "Olena", Role = Role.Manager });
        await _viewModel.AppearCommand.ExecuteAsync(null);
        Assert.True(_viewModel.IsCameraActive);

        _viewModel.ScanDetectedCommand.Execute("hello");
        Assert.True(_viewModel.IsCameraActive);
        _viewModel.ScanDetectedCommand.Execute(ValidBarcode);
        _viewModel.ScanDetectedCommand.Execute("3017620422003");

        Assert.Equal(ValidBarcode, _viewModel.Barcode);
        Assert.False(_viewModel.IsCameraActive);
        Assert.True(_viewModel.CanStartScan);
    }

    [Fact]
    public async Task NoCamera_ShowsTheBanner()
    {
        _scanner.IsCameraSupported = false;

        await _viewModel.AppearCommand.ExecuteAsync(null);

        Assert.Equal(CameraMessages.Unsupported, _viewModel.CameraMessage);
        Assert.False(_viewModel.CanStartScan);
    }

    [Fact]
    public async Task CameraThatCannotStart_ShowsTheBannerWithTryAgain()
    {
        await _viewModel.AppearCommand.ExecuteAsync(null);

        _scanner.ReportStartFailure();

        Assert.False(_viewModel.IsCameraActive);
        Assert.Equal((CameraMessages.Failed, CameraMessages.RetryAction),
            (_viewModel.CameraMessage, _viewModel.CameraActionText));
    }

    [Fact]
    public async Task Lookup_FillsTheNameOnlyWhenItIsEmpty()
    {
        _currentUser.SignIn(new User { FullName = "Olena", Role = Role.Manager });
        _lookup.Result = LookupResult.Found("Nutella", "Ferrero", "400 g", "Spreads");
        await _viewModel.AppearCommand.ExecuteAsync(null);

        _viewModel.Barcode = "3017620422003";
        Assert.Equal(("Nutella", _lookup.Attribution), (_viewModel.Name, _viewModel.DataSource));
        Assert.False(_viewModel.QuickSearch.IsOpen);

        _viewModel.Name = "My own name";
        _viewModel.Barcode = ValidBarcode;

        Assert.Equal("My own name", _viewModel.Name);
        Assert.Single(_lookup.Asked);
    }

    [Fact]
    public async Task Lookup_Offline_SaysSoAndLeavesTheNameEmpty()
    {
        _currentUser.SignIn(new User { FullName = "Olena", Role = Role.Manager });
        _lookup.Result = LookupResult.Offline();
        await _viewModel.AppearCommand.ExecuteAsync(null);

        _viewModel.Barcode = "3017620422003";

        Assert.Equal(string.Empty, _viewModel.Name);
        Assert.Contains("Cannot reach Open Food Facts", _viewModel.LookupText);
    }

    [Fact]
    public async Task Duplicate_ByBarcodeOrName_IsShownAndCanBeOpened()
    {
        await SignInAsManagerWithADrillAsync();

        _viewModel.Barcode = ValidBarcode;
        Assert.Equal($"Already in the catalog: Drill ({ValidBarcode})", _viewModel.DuplicateText);
        Assert.Empty(_lookup.Asked);

        _viewModel.Barcode = string.Empty;
        Assert.False(_viewModel.HasDuplicate);
        _viewModel.Name = " drill ";
        Assert.True(_viewModel.HasDuplicate);

        await _viewModel.OpenDuplicateCommand.ExecuteAsync(null);
        Assert.Equal(Routes.ProductDetail, Assert.Single(_navigation.RequestedRoutes));
    }

    [Fact]
    public async Task TypingTheName_SuggestsAndPickingWarnsWithoutChangingTheText()
    {
        await SignInAsManagerWithADrillAsync();

        _viewModel.QuickSearch.Query = "dri";
        Assert.Equal("dri", _viewModel.Name);
        Assert.True(_viewModel.QuickSearch.IsOpen);

        _viewModel.QuickSearch.PickCommand.Execute(_viewModel.QuickSearch.Suggestions.Single());

        Assert.Equal("dri", _viewModel.Name);
        Assert.False(_viewModel.QuickSearch.IsOpen);
        Assert.StartsWith("Already in the catalog: Drill", _viewModel.DuplicateText);

        _viewModel.QuickSearch.Query = "hammer";
        Assert.False(_viewModel.QuickSearch.IsOpen);
        Assert.False(_viewModel.HasDuplicate);
    }

    [Fact]
    public void SaveText_AndHint_FollowTheState()
    {
        _currentUser.SignIn(new User { FullName = "Olena", Role = Role.Manager });
        Assert.Equal("Add product", _viewModel.SaveText);
        Assert.NotEqual(string.Empty, _viewModel.SaveHint);

        FillValidForm();
        Assert.Equal(string.Empty, _viewModel.SaveHint);

        _viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = new ProductSummary(Guid.NewGuid(), "Saw", ValidBarcode, 0)
        });
        Assert.Equal("Save changes", _viewModel.SaveText);
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

    [Fact]
    public void Typing_SavesADraftOfNameAndBarcode()
    {
        _viewModel.Name = "Cordless Saw";
        _viewModel.Barcode = ValidBarcode;

        Assert.Equal(new ProductFormDraft("Cordless Saw", ValidBarcode), _appState.GetDraft<ProductFormDraft>());
    }

    [Fact]
    public void ClearingBothFields_RemovesTheDraft()
    {
        FillValidForm();

        _viewModel.Name = string.Empty;
        _viewModel.Barcode = string.Empty;

        Assert.Null(_appState.GetDraft<ProductFormDraft>());
    }

    [Fact]
    public void OnAppearing_RestoresTheDraftOnce()
    {
        _appState.SetDraft(new ProductFormDraft("Cordless Saw", ValidBarcode));
        var viewModel = CreateViewModel(_appState);

        viewModel.OnAppearing();

        Assert.Equal("Cordless Saw", viewModel.Name);
        Assert.Equal(ValidBarcode, viewModel.Barcode);
    }

    [Fact]
    public void OnAppearing_AgainAfterReturningFromAnotherPage_DoesNotOverwriteTyping()
    {
        _appState.SetDraft(new ProductFormDraft("Cordless Saw", ValidBarcode));
        var viewModel = CreateViewModel(_appState);
        viewModel.OnAppearing();
        viewModel.Name = "Changed";

        _appState.SetDraft(new ProductFormDraft("Stale", ValidBarcode));
        viewModel.OnAppearing();

        Assert.Equal("Changed", viewModel.Name);
    }

    [Fact]
    public void OnAppearing_WithPrefilledBarcode_IgnoresTheDraft()
    {
        _appState.SetDraft(new ProductFormDraft("Cordless Saw", ValidBarcode));
        var viewModel = CreateViewModel(_appState);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationKeys.Barcode] = "4820001000016" });

        viewModel.OnAppearing();

        Assert.Equal(string.Empty, viewModel.Name);
        Assert.Equal("4820001000016", viewModel.Barcode);
    }

    [Fact]
    public void ApplyQueryAttributes_FromTheLookup_PrefillsNameBarcodeAndAttribution()
    {
        _appState.SetDraft(new ProductFormDraft("Old draft", ValidBarcode));
        var viewModel = CreateViewModel(_appState);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.Barcode] = "3017620422003",
            [NavigationKeys.ProductName] = "Nutella",
            [NavigationKeys.DataSource] = "Data: Open Food Facts (ODbL)"
        });

        viewModel.OnAppearing();

        Assert.Equal("Nutella", viewModel.Name);
        Assert.Equal("3017620422003", viewModel.Barcode);
        Assert.Equal("Data: Open Food Facts (ODbL)", viewModel.DataSource);
    }

    [Fact]
    public async Task Save_Success_ClearsTheDraft()
    {
        _currentUser.SignIn(new User { FullName = "Olena Kovalenko", Role = Role.Manager });
        FillValidForm();

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Null(_appState.GetDraft<ProductFormDraft>());
    }

    [Fact]
    public async Task Cancel_WithTypedInput_AsksAndStaysWhenRefused()
    {
        FillValidForm();
        _confirm.Answer = false;

        await _viewModel.CancelCommand.ExecuteAsync(null);

        Assert.Equal("Discard the new product?", Assert.Single(_confirm.Asked).Title);
        Assert.Empty(_navigation.RequestedRoutes);
        Assert.NotNull(_appState.GetDraft<ProductFormDraft>());
    }

    [Fact]
    public async Task Cancel_OfAnEmptyForm_AsksNothing()
    {
        await _viewModel.CancelCommand.ExecuteAsync(null);

        Assert.Empty(_confirm.Asked);
        Assert.Equal(Routes.Back, Assert.Single(_navigation.RequestedRoutes));
    }

    [Fact]
    public async Task Cancel_ClearsTheDraft()
    {
        FillValidForm();

        await _viewModel.CancelCommand.ExecuteAsync(null);

        Assert.Null(_appState.GetDraft<ProductFormDraft>());
    }

    private void FillValidForm()
    {
        _viewModel.Name = "Cordless Saw";
        _viewModel.Barcode = ValidBarcode;
    }

    [Fact]
    public async Task Edit_SavesNewNameWithTheSameBarcode_AndReturnsTheRowWithItsQuantity()
    {
        var manager = new User { FullName = "Olena", Role = Role.Manager };
        _currentUser.SignIn(manager);
        var saw = new Product { Name = "Saw", Barcode = ValidBarcode, CreatedByUserId = manager.Id };
        await _products.AddAsync(saw);
        var row = new ProductSummary(saw.Id, "Saw", ValidBarcode, 7);

        _viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = row });
        Assert.Equal("Edit product", _viewModel.PageTitle);
        Assert.Equal("Saw", _viewModel.Name);
        _viewModel.Name = "Circular Saw";
        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Circular Saw", (await _products.GetByIdAsync(saw.Id))!.Name);
        var returned = (ProductSummary)_navigation.RequestedParameters.Last()![NavigationKeys.UpdatedProduct];
        Assert.Equal(("Circular Saw", 7), (returned.Name, returned.Quantity));
    }

    [Fact]
    public async Task Edit_DoesNotTouchTheNewProductDraft()
    {
        var manager = new User { FullName = "Olena", Role = Role.Manager };
        _currentUser.SignIn(manager);
        _appState.SetDraft(new ProductFormDraft("Half typed", "123"));
        var saw = new Product { Name = "Saw", Barcode = ValidBarcode, CreatedByUserId = manager.Id };
        await _products.AddAsync(saw);

        _viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = new ProductSummary(saw.Id, "Saw", ValidBarcode, 0)
        });
        _viewModel.Name = "Other";
        await _viewModel.CancelCommand.ExecuteAsync(null);

        Assert.Equal(new ProductFormDraft("Half typed", "123"), _appState.GetDraft<ProductFormDraft>());
    }

    [Fact]
    public async Task Edit_BarcodeOfAnotherProduct_ShowsTheErrorAndKeepsThePage()
    {
        var manager = new User { FullName = "Olena", Role = Role.Manager };
        _currentUser.SignIn(manager);
        var saw = new Product { Name = "Saw", Barcode = ValidBarcode, CreatedByUserId = manager.Id };
        var drillBarcode = Ean13.WithCheckDigit("400638133394");
        await _products.AddAsync(saw);
        await _products.AddAsync(new Product { Name = "Drill", Barcode = drillBarcode, CreatedByUserId = manager.Id });
        _viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = new ProductSummary(saw.Id, "Saw", ValidBarcode, 7)
        });

        _viewModel.Barcode = drillBarcode;
        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains("already exists", _viewModel.BarcodeError);
        Assert.Equal(ValidBarcode, (await _products.GetByIdAsync(saw.Id))!.Barcode);
        Assert.DoesNotContain(_navigation.RequestedRoutes, route => route == Routes.Back);
    }

    [Fact]
    public void Edit_BlankName_DisablesSave()
    {
        var manager = new User { FullName = "Olena", Role = Role.Manager };
        _currentUser.SignIn(manager);
        _viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = new ProductSummary(Guid.NewGuid(), "Saw", ValidBarcode, 0)
        });

        _viewModel.Name = "  ";

        Assert.Equal("Name is required.", _viewModel.NameError);
        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }
}
