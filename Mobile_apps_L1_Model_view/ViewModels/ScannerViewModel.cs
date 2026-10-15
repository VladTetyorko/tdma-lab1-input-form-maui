using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Scan (camera) or type a barcode: a known product opens the operation page, an unknown barcode
// offers a Manager to add the product. The camera runs only while the page is visible.
public partial class ScannerViewModel(
    IBarcodeScanner scanner,
    IProductCatalog catalog,
    ICurrentUserService currentUser,
    INavigationService navigation) : ObservableObject, IPageLifecycleAware
{
    // Set when this page sent the user to a page that can change products or stock
    private bool _changed;

    public bool IsCameraSupported => scanner.IsCameraSupported;

    public bool IsCameraUnavailable => !scanner.IsCameraSupported;

    // The camera view binds to this: true = camera on and reading, false = camera released
    [ObservableProperty]
    public partial bool IsCameraActive { get; set; }

    [ObservableProperty]
    public partial string ManualBarcode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    // The barcode that matched no product; empty = none
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnknownBarcode))]
    [NotifyPropertyChangedFor(nameof(CanScanAgain))]
    [NotifyPropertyChangedFor(nameof(CanCreateProduct))]
    [NotifyCanExecuteChangedFor(nameof(CreateProductCommand))]
    public partial string UnknownBarcode { get; set; } = string.Empty;

    public bool HasUnknownBarcode => UnknownBarcode.Length > 0;

    // Scanning again makes sense only with a camera; without one the user retypes the barcode
    public bool CanScanAgain => HasUnknownBarcode && scanner.IsCameraSupported;

    public bool CanCreateProduct =>
        HasUnknownBarcode && currentUser.CurrentUser is { } user && RolePermissions.CanManageProducts(user.Role);

    public void OnAppearing() => StartCommand.Execute(null);

    // The camera must not keep running behind another page or in the background
    public void OnDisappearing() => IsCameraActive = false;

    [RelayCommand]
    private async Task StartAsync()
    {
        StatusMessage = string.Empty;
        UnknownBarcode = string.Empty;
        ManualBarcode = string.Empty;

        if (!scanner.IsCameraSupported)
        {
            return;
        }

        IsCameraActive = await scanner.RequestCameraAccessAsync();
        if (!IsCameraActive)
        {
            StatusMessage = "Camera access was denied. Type the barcode instead.";
        }
    }

    // The camera view calls this for every barcode it reads; only the first one counts
    [RelayCommand]
    private Task BarcodeDetectedAsync(string? barcode)
    {
        if (!IsCameraActive || string.IsNullOrWhiteSpace(barcode))
        {
            return Task.CompletedTask;
        }

        IsCameraActive = false;
        return LookUpBarcodeAsync(barcode);
    }

    [RelayCommand]
    private Task LookUpAsync() => LookUpBarcodeAsync(ManualBarcode);

    [RelayCommand]
    private Task ScanAgainAsync() => StartAsync();

    [RelayCommand(CanExecute = nameof(CanCreateProduct))]
    private Task CreateProductAsync()
    {
        _changed = true;
        var barcode = UnknownBarcode;
        UnknownBarcode = string.Empty;
        return navigation.GoToAsync(Routes.ProductForm, new Dictionary<string, object> { [NavigationKeys.Barcode] = barcode });
    }

    // Tells the list to reload only when something may have changed
    [RelayCommand]
    private Task GoBackAsync() =>
        _changed
            ? navigation.GoToAsync(Routes.Back, new Dictionary<string, object> { [NavigationKeys.ProductsChanged] = true })
            : navigation.GoToAsync(Routes.Back);

    private async Task LookUpBarcodeAsync(string? text)
    {
        var barcode = text?.Trim() ?? string.Empty;
        if (barcode.Length == 0)
        {
            StatusMessage = "Enter a barcode.";
            return;
        }

        var product = await catalog.GetSummaryByBarcodeAsync(barcode);
        if (product is not null)
        {
            _changed = true;
            StatusMessage = string.Empty;
            await navigation.GoToAsync(Routes.Operation,
                new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = product });
            return;
        }

        UnknownBarcode = barcode;
        StatusMessage = CanCreateProduct
            ? $"No product has barcode {barcode}. You can add it."
            : $"No product has barcode {barcode}. Ask a manager to add it.";
    }
}
