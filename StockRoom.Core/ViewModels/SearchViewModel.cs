using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Services.Lookup;

namespace Mobile_apps_L1_Model_view.ViewModels;

// "This one product, now": the camera on top, a field with at most five results below. A known code opens the product
// page at once; an unknown one is looked up in the open databases and the Manager may create it
public sealed partial class SearchViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly IProductCatalog _catalog;
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionGuard _guard;
    private readonly INavigationService _navigation;
    private readonly IBarcodeScanner _scanner;
    private readonly LatestLookup _lookup;
    private readonly IScanFeedback _feedback;
    private readonly ILogger<SearchViewModel> _logger;
    private readonly ScanDebouncer _debouncer;
    private LookupResult? _found;

    public SearchViewModel(
        IProductCatalog catalog,
        ICurrentUserService currentUser,
        IPermissionGuard guard,
        INavigationService navigation,
        IBarcodeScanner scanner,
        IProductLookup lookup,
        IScanFeedback feedback,
        IClock clock,
        QuickSearchViewModel quickSearch,
        ILogger<SearchViewModel> logger)
    {
        _catalog = catalog;
        _currentUser = currentUser;
        _guard = guard;
        _navigation = navigation;
        _scanner = scanner;
        _lookup = new LatestLookup(lookup);
        _feedback = feedback;
        _logger = logger;
        _debouncer = new ScanDebouncer(clock);
        QuickSearch = quickSearch;
        quickSearch.Picked += async (_, product) => await OpenProductAsync(product);
        quickSearch.SubmittedText += async (_, text) => await FindAsync(text);
        quickSearch.PropertyChanged += (_, e) =>
        {
            // Typing something new puts the message about the last code away
            if (e.PropertyName == nameof(QuickSearchViewModel.Query) && quickSearch.Query.Length > 0
                && quickSearch.Query != UnknownBarcode)
            {
                _lookup.Cancel();
                UnknownBarcode = string.Empty;
            }
        };
    }

    public QuickSearchViewModel QuickSearch { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CameraMessage))]
    [NotifyPropertyChangedFor(nameof(CameraActionText))]
    [NotifyPropertyChangedFor(nameof(IsCameraActive))]
    public partial CameraState CameraState { get; set; }

    // Set by a phone page while the field has the focus: the camera makes room for the results
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCameraActive))]
    public partial bool IsTyping { get; set; }

    public bool IsCameraActive => CameraState == CameraState.Active && !IsTyping;

    public string CameraMessage => CameraMessages.For(CameraState);

    public string CameraActionText => CameraMessages.ActionFor(CameraState);

    // The code that matched no product; empty = none
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnknown))]
    [NotifyPropertyChangedFor(nameof(UnknownText))]
    [NotifyPropertyChangedFor(nameof(CanCreateUnknown))]
    public partial string UnknownBarcode { get; set; } = string.Empty;

    public bool IsUnknown => UnknownBarcode.Length > 0;

    public bool CanCreateUnknown => IsUnknown && MayCreate;

    public string UnknownText => !IsUnknown ? string.Empty
        : MayCreate ? $"{UnknownBarcode} is not in the catalogue."
        : $"{UnknownBarcode} is not in the catalogue. Ask the manager to add it.";

    // What the open databases said about the unknown code, with the attribution of the one that knew it
    [ObservableProperty]
    public partial string LookupText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    private bool MayCreate => _currentUser.CurrentUser is { } user && RolePermissions.CanManageProducts(user.Role);

    public void OnAppearing() => AppearCommand.Execute(null);

    public void OnDisappearing()
    {
        _lookup.Cancel();
        if (LookupText == LookupMessages.Searching)
        {
            LookupText = string.Empty;
        }

        _scanner.Failed -= OnCameraFailed;
        CameraState = CameraState.Off;
        IsTyping = false;
    }

    // The Search tab tapped again: an empty field
    public void Reset()
    {
        _lookup.Cancel();
        QuickSearch.Query = string.Empty;
        QuickSearch.CloseCommand.Execute(null);
        UnknownBarcode = string.Empty;
        LookupText = string.Empty;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task AppearAsync()
    {
        // A product may have been created since the code was found to be unknown
        if (IsUnknown && await _catalog.GetSummaryByBarcodeAsync(UnknownBarcode) is not null)
        {
            Reset();
        }

        await QuickSearch.LoadCommand.ExecuteAsync(null);
        await StartCameraAsync();
    }

    [RelayCommand]
    private Task RetryCameraAsync() => StartCameraAsync();

    // The camera reports the same code again and again while it is in view
    [RelayCommand]
    private Task ScanDetectedAsync(string? text) =>
        text is not null && _debouncer.Accept(text) ? FindAsync(text) : Task.CompletedTask;

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (!IsUnknown)
        {
            return;
        }

        if (await _guard.CheckAsync(RolePermissions.CanManageProducts, "add products") is { } refusal)
        {
            StatusMessage = refusal;
            return;
        }

        await _navigation.GoToAsync(Routes.NewProduct, LookupParameters.ForNewProduct(UnknownBarcode, _found));
    }

    private async Task FindAsync(string? text)
    {
        var parsed = ScanPayloadParser.Parse(text);
        if (!parsed.IsSuccess)
        {
            StatusMessage = parsed.Error!;
            _logger.LogWarning("Scanned text was not a barcode: {Reason}", parsed.Error);
            _feedback.Error();
            return;
        }

        var barcode = parsed.Value!;
        StatusMessage = string.Empty;
        if (await _catalog.GetSummaryByBarcodeAsync(barcode) is { } product)
        {
            _logger.LogInformation("Barcode {Barcode} matched product {ProductId}", barcode, product.Id);
            _feedback.Success();
            _lookup.Cancel();
            QuickSearch.Query = string.Empty;
            UnknownBarcode = string.Empty;
            await OpenProductAsync(product);
            return;
        }

        _logger.LogInformation("Barcode {Barcode} matched no product", barcode);
        _feedback.Error();
        QuickSearch.Query = string.Empty;
        _found = null;
        LookupText = LookupMessages.Searching;
        UnknownBarcode = barcode;
        await LookUpAsync(barcode);
    }

    // Offline, a timeout or a bad answer only show a message
    private async Task LookUpAsync(string barcode)
    {
        if (await _lookup.FindAsync(barcode) is not { } result || UnknownBarcode != barcode)
        {
            return;
        }

        if (!result.IsFound)
        {
            LookupText = LookupMessages.Failure(result.Status, barcode, MayCreate);
            _logger.LogWarning("Lookup of barcode {Barcode} gave {Status}", barcode, result.Status);
            return;
        }

        _found = result;
        LookupText = $"{LookupMessages.Found(result)}\n{result.Attribution}";
    }

    private Task OpenProductAsync(ProductSummary product) =>
        _navigation.GoToAsync(Routes.ProductDetail,
            new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = product });

    private async Task StartCameraAsync()
    {
        if (!_scanner.IsCameraSupported)
        {
            CameraState = CameraState.Unsupported;
        }
        else if (await _scanner.RequestCameraAccessAsync())
        {
            _scanner.Failed -= OnCameraFailed;
            _scanner.Failed += OnCameraFailed;
            CameraState = CameraState.Active;
        }
        else
        {
            CameraState = CameraState.Denied;
            _logger.LogWarning("Camera access was denied, the barcode must be typed");
        }
    }

    private void OnCameraFailed(object? sender, EventArgs e)
    {
        CameraState = CameraState.Failed;
        _logger.LogWarning("The camera could not start, the barcode must be typed");
    }
}
