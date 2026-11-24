using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Scan (camera: barcode or QR) or type a barcode: a known product opens the operation page, an unknown barcode
// offers a Manager to add the product, by hand or prefilled from Open Food Facts.
// The camera runs only while the page is visible.
public partial class ScannerViewModel(
    IBarcodeScanner scanner,
    IProductCatalog catalog,
    ICurrentUserService currentUser,
    INavigationService navigation,
    IProductLookup lookup) : ObservableObject, IPageLifecycleAware
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
    [NotifyCanExecuteChangedFor(nameof(CreateFromLookupCommand))]
    public partial string UnknownBarcode { get; set; } = string.Empty;

    public bool HasUnknownBarcode => UnknownBarcode.Length > 0;

    // Scanning again makes sense only with a camera; without one the user retypes the barcode
    public bool CanScanAgain => HasUnknownBarcode && scanner.IsCameraSupported;

    public bool CanCreateProduct =>
        HasUnknownBarcode && currentUser.CurrentUser is { } user && RolePermissions.CanManageProducts(user.Role);

    // Attribution for the open data the lookup button uses
    public string LookupAttribution => lookup.Attribution;

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

    // Manager only: asks Open Food Facts about the unknown barcode and opens the form prefilled with the answer.
    // One request per tap; offline, a timeout or a bad answer only show a message
    [RelayCommand(CanExecute = nameof(CanCreateProduct))]
    private async Task CreateFromLookupAsync()
    {
        var barcode = UnknownBarcode;
        var result = await lookup.FindAsync(barcode);
        if (!result.IsFound)
        {
            StatusMessage = LookupFailureMessage(result.Status, barcode);
            return;
        }

        _changed = true;
        UnknownBarcode = string.Empty;
        StatusMessage = string.Empty;
        await navigation.GoToAsync(Routes.ProductForm, new Dictionary<string, object>
        {
            [NavigationKeys.Barcode] = barcode,
            [NavigationKeys.ProductName] = result.Name,
            [NavigationKeys.DataSource] = lookup.Attribution
        });
    }

    private static string LookupFailureMessage(LookupStatus status, string barcode) => status switch
    {
        LookupStatus.NotFound => $"Open Food Facts does not know barcode {barcode}. You can add the product by hand.",
        LookupStatus.Offline => "Cannot reach Open Food Facts (offline or too slow). Try again later or add the product by hand.",
        _ => "Open Food Facts gave no usable answer. Try again later or add the product by hand."
    };

    // Tells the list to reload only when something may have changed
    [RelayCommand]
    private Task GoBackAsync() =>
        _changed
            ? navigation.GoToAsync(Routes.Back, new Dictionary<string, object> { [NavigationKeys.ProductsChanged] = true })
            : navigation.GoToAsync(Routes.Back);

    private async Task LookUpBarcodeAsync(string? text)
    {
        var parsed = ScanPayloadParser.Parse(text);
        if (!parsed.IsSuccess)
        {
            StatusMessage = parsed.Error!;
            return;
        }

        var barcode = parsed.Value!;

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
