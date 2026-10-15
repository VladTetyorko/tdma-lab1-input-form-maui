using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class ScannerViewModelTests
{
    private const string UnknownBarcode = "4006381333931";

    private readonly TestStore _store = new();
    private readonly FakeBarcodeScanner _scanner = new();
    private readonly string _drillBarcode = Ean13.WithCheckDigit("482000100001");
    private readonly ScannerViewModel _viewModel;

    public ScannerViewModelTests()
    {
        _viewModel = new ScannerViewModel(_scanner, _store.Catalog, _store.CurrentUser, _store.Navigation);
    }

    [Fact]
    public async Task OnAppearing_RequestsCameraAndStartsIt()
    {
        _viewModel.OnAppearing();

        Assert.Equal(1, _scanner.AccessRequests);
        Assert.True(_viewModel.IsCameraActive);
        Assert.Equal(string.Empty, _viewModel.StatusMessage);
    }

    [Fact]
    public async Task OnAppearing_CameraDenied_StaysOffAndExplainsTheManualWay()
    {
        _scanner.CameraAccessGranted = false;

        _viewModel.OnAppearing();

        Assert.False(_viewModel.IsCameraActive);
        Assert.Equal("Camera access was denied. Type the barcode instead.", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task OnAppearing_WithoutCameraSupport_DoesNotAskForPermission()
    {
        _scanner.IsCameraSupported = false;

        _viewModel.OnAppearing();

        Assert.Equal(0, _scanner.AccessRequests);
        Assert.False(_viewModel.IsCameraActive);
        Assert.True(_viewModel.IsCameraUnavailable);
    }

    [Fact]
    public async Task OnDisappearing_TurnsTheCameraOff()
    {
        _viewModel.OnAppearing();

        _viewModel.OnDisappearing();

        Assert.False(_viewModel.IsCameraActive);
    }

    [Fact]
    public async Task BarcodeDetected_WhileCameraIsOff_IsIgnored()
    {
        await _store.AddProductAsync("Drill", _drillBarcode);

        await _viewModel.BarcodeDetectedCommand.ExecuteAsync(_drillBarcode);

        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task BarcodeDetected_OnlyTheFirstReadCounts()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var drill = await _store.AddProductAsync("Drill", _drillBarcode);
        _viewModel.OnAppearing();

        await _viewModel.BarcodeDetectedCommand.ExecuteAsync(_drillBarcode);
        await _viewModel.BarcodeDetectedCommand.ExecuteAsync(UnknownBarcode);

        Assert.Equal(Routes.Operation, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal(drill.Id, SelectedProduct().Id);
        Assert.False(_viewModel.HasUnknownBarcode);
    }

    [Fact]
    public async Task BarcodeDetected_EmptyText_IsIgnoredAndCameraStaysOn()
    {
        _viewModel.OnAppearing();

        await _viewModel.BarcodeDetectedCommand.ExecuteAsync("   ");

        Assert.True(_viewModel.IsCameraActive);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task LookUp_BlankBarcode_AsksForOne()
    {
        await _viewModel.LookUpCommand.ExecuteAsync(null);

        Assert.Equal("Enter a barcode.", _viewModel.StatusMessage);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task LookUp_KnownBarcode_OpensOperationWithTheProduct()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var drill = await _store.AddProductAsync("Drill", _drillBarcode);
        _viewModel.ManualBarcode = $"  {_drillBarcode} ";

        await _viewModel.LookUpCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Operation, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal(drill.Id, SelectedProduct().Id);
        Assert.Equal(string.Empty, _viewModel.StatusMessage);
    }

    [Fact]
    public async Task LookUp_UnknownBarcode_OffersToAddItToAManager()
    {
        await _store.SignInAsync(Role.Manager);
        _viewModel.ManualBarcode = UnknownBarcode;

        await _viewModel.LookUpCommand.ExecuteAsync(null);

        Assert.Equal(UnknownBarcode, _viewModel.UnknownBarcode);
        Assert.True(_viewModel.HasUnknownBarcode);
        Assert.Equal($"No product has barcode {UnknownBarcode}. You can add it.", _viewModel.StatusMessage);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task LookUp_UnknownBarcode_NonManagerIsTold_ToAskAManager(Role role)
    {
        await _store.SignInAsync(role);
        _viewModel.ManualBarcode = UnknownBarcode;

        await _viewModel.LookUpCommand.ExecuteAsync(null);

        Assert.False(_viewModel.CanCreateProduct);
        Assert.False(_viewModel.CreateProductCommand.CanExecute(null));
        Assert.Equal($"No product has barcode {UnknownBarcode}. Ask a manager to add it.", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task CreateProduct_ForManager_OpensTheFormPrefilledWithTheBarcode()
    {
        await _store.SignInAsync(Role.Manager);
        _viewModel.ManualBarcode = UnknownBarcode;
        await _viewModel.LookUpCommand.ExecuteAsync(null);
        Assert.True(_viewModel.CreateProductCommand.CanExecute(null));

        await _viewModel.CreateProductCommand.ExecuteAsync(null);

        Assert.Equal(Routes.ProductForm, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal(UnknownBarcode, _store.Navigation.RequestedParameters.Last()![NavigationKeys.Barcode]);
        Assert.False(_viewModel.HasUnknownBarcode);
    }

    [Fact]
    public async Task CanScanAgain_OnlyWithCameraAndAnUnknownBarcode()
    {
        await _store.SignInAsync(Role.Manager);
        Assert.False(_viewModel.CanScanAgain);

        _viewModel.ManualBarcode = UnknownBarcode;
        await _viewModel.LookUpCommand.ExecuteAsync(null);
        Assert.True(_viewModel.CanScanAgain);

        _scanner.IsCameraSupported = false;
        Assert.False(_viewModel.CanScanAgain);
    }

    [Fact]
    public async Task ScanAgain_ClearsTheUnknownBarcodeAndStartsTheCameraAgain()
    {
        await _store.SignInAsync(Role.Manager);
        _viewModel.ManualBarcode = UnknownBarcode;
        await _viewModel.LookUpCommand.ExecuteAsync(null);

        await _viewModel.ScanAgainCommand.ExecuteAsync(null);

        Assert.False(_viewModel.HasUnknownBarcode);
        Assert.True(_viewModel.IsCameraActive);
        Assert.Equal(1, _scanner.AccessRequests);
    }

    [Fact]
    public async Task GoBack_AfterNothingWasOpened_SendsPlainBack()
    {
        await _viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Back, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Null(Assert.Single(_store.Navigation.RequestedParameters));
    }

    [Fact]
    public async Task GoBack_AfterAKnownProductWasOpened_TellsTheListToReload()
    {
        await _store.SignInAsync(Role.Storekeeper);
        await _store.AddProductAsync("Drill", _drillBarcode);
        _viewModel.ManualBarcode = _drillBarcode;
        await _viewModel.LookUpCommand.ExecuteAsync(null);

        await _viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Back, _store.Navigation.RequestedRoutes.Last());
        Assert.True((bool)_store.Navigation.RequestedParameters.Last()![NavigationKeys.ProductsChanged]);
    }

    [Fact]
    public async Task GoBack_AfterAProductWasCreated_TellsTheListToReload()
    {
        await _store.SignInAsync(Role.Manager);
        _viewModel.ManualBarcode = UnknownBarcode;
        await _viewModel.LookUpCommand.ExecuteAsync(null);
        await _viewModel.CreateProductCommand.ExecuteAsync(null);

        await _viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.True((bool)_store.Navigation.RequestedParameters.Last()![NavigationKeys.ProductsChanged]);
    }

    [Fact]
    public void IsCameraSupported_MirrorsTheScanner()
    {
        Assert.True(_viewModel.IsCameraSupported);
        Assert.False(_viewModel.IsCameraUnavailable);
    }


    private ProductSummary SelectedProduct() =>
        Assert.IsType<ProductSummary>(_store.Navigation.RequestedParameters.Last()![NavigationKeys.SelectedProduct]);
}
