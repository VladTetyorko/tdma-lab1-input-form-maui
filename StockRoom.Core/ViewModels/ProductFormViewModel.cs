using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Add/edit-product form. Opened from the list (add, or edit with the product in the navigation parameters),
// or from the scanner with an unknown barcode (prefilled).
// A new product's typed text is kept as a draft in IAppStateService until it is saved or cancelled;
// editing keeps no draft
public partial class ProductFormViewModel : ObservableObject, INavigationParameterReceiver, IPageLifecycleAware
{
    private readonly IProductValidator validator;
    private readonly IProductService productService;
    private readonly ICurrentUserService currentUser;
    private readonly IPermissionGuard guard;
    private readonly IAppStateService appState;
    private readonly INavigationService navigation;
    private readonly IBarcodeScanner scanner;
    private readonly IProductLookup lookup;
    private readonly ILogger<ProductFormViewModel> logger;
    private bool _opened;
    private bool _prefilled;
    private string _lookedUp = string.Empty;

    // A suggestion the user tapped under the name
    private ProductSummary? _picked;

    // The product being edited; null when adding
    private ProductSummary? _editing;

    public ProductFormViewModel(
        IProductValidator validator,
        IProductService productService,
        ICurrentUserService currentUser,
        IPermissionGuard guard,
        IAppStateService appState,
        INavigationService navigation,
        ProductPhotoEditor photo,
        IBarcodeScanner scanner,
        IProductLookup lookup,
        QuickSearchViewModel quickSearch,
        ILogger<ProductFormViewModel> logger)
    {
        this.validator = validator;
        this.productService = productService;
        this.currentUser = currentUser;
        this.guard = guard;
        this.appState = appState;
        this.navigation = navigation;
        this.scanner = scanner;
        this.lookup = lookup;
        this.logger = logger;
        Photo = photo;
        QuickSearch = quickSearch;
        quickSearch.IsSelectMode = true;
        quickSearch.PropertyChanged += (_, e) =>
        {
            // Typing in the name field arrives here
            if (e.PropertyName == nameof(QuickSearchViewModel.Query) && Name != quickSearch.Query)
            {
                Name = quickSearch.Query;
            }
        };
        quickSearch.Picked += (_, product) =>
        {
            _picked = product;
            UpdateDuplicate();
        };
    }

    public string PageTitle => _editing is null ? "New product" : "Edit product";

    // The main button of the page
    public string SaveText => _editing is null ? "Add product" : "Save changes";

    // Why it is disabled, or empty
    public string SaveHint => CanSave() ? string.Empty : "Enter a name and a valid barcode first";

    // The name field with its suggestions of products that already exist
    public QuickSearchViewModel QuickSearch { get; }

    // The photo of the product (staged until Save, see ProductPhotoEditor)
    public ProductPhotoEditor Photo { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CameraMessage))]
    [NotifyPropertyChangedFor(nameof(CameraActionText))]
    [NotifyPropertyChangedFor(nameof(IsCameraActive))]
    [NotifyPropertyChangedFor(nameof(CanStartScan))]
    public partial CameraState CameraState { get; set; }

    // Set by a phone page while a field has the focus
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCameraActive))]
    public partial bool IsTyping { get; set; }

    public bool IsCameraActive => CameraState == CameraState.Active && !IsTyping;

    public bool CanStartScan => scanner.IsCameraSupported && CameraState == CameraState.Off;

    public string CameraMessage => CameraMessages.For(CameraState);

    public string CameraActionText => CameraMessages.ActionFor(CameraState);

    // What Open Food Facts said about the barcode
    [ObservableProperty]
    public partial string LookupText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDuplicate))]
    public partial string DuplicateText { get; set; } = string.Empty;

    public bool HasDuplicate => DuplicateText.Length > 0;

    private ProductSummary? Duplicate { get; set; }

    // Photo buttons are for the Manager (the form is too, but the check is made here as well)
    public bool CanManagePhotos => currentUser.CurrentUser is { } user && RolePermissions.CanManagePhotos(user.Role);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyPropertyChangedFor(nameof(SaveHint))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyPropertyChangedFor(nameof(SaveHint))]
    public partial string Barcode { get; set; } = string.Empty;

    // Empty string = no error (keeps XAML bindings simple)
    [ObservableProperty]
    public partial string NameError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BarcodeError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    // Attribution for prefilled open data (empty when the user typed everything)
    [ObservableProperty]
    public partial string DataSource { get; set; } = string.Empty;

    partial void OnNameChanged(string value)
    {
        NameError = validator.ValidateName(value) ?? string.Empty;
        _picked = null;

        // Set by the app (a draft, a lookup), not typed: no drop-down for it
        if (QuickSearch.Query != value)
        {
            QuickSearch.Query = value;
            QuickSearch.CloseCommand.Execute(null);
        }

        UpdateDuplicate();
        SaveDraft();
    }

    partial void OnBarcodeChanged(string value)
    {
        BarcodeError = validator.ValidateBarcodeFormat(value) ?? string.Empty;
        LookupText = string.Empty;
        UpdateDuplicate();
        SaveDraft();
        LookupCommand.Execute(null);
    }

    // The scanner passes the unknown barcode it could not find; the list passes the product to edit
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationKeys.SelectedProduct, out var selected) && selected is ProductSummary product)
        {
            _editing = product;
            QuickSearch.ExcludedProductId = product.Id;
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(SaveText));
            Name = product.Name;
            Barcode = product.Barcode;
            Photo.Load(product.PhotoPath);
        }

        if (query.TryGetValue(NavigationKeys.Barcode, out var value) && value is string barcode)
        {
            _prefilled = true;
            Barcode = barcode;
        }

        if (query.TryGetValue(NavigationKeys.ProductName, out var name) && name is string productName)
        {
            _prefilled = true;
            Name = productName;
        }

        if (query.TryGetValue(NavigationKeys.DataSource, out var source) && source is string dataSource)
        {
            DataSource = dataSource;
        }
    }

    public void OnAppearing() => AppearCommand.Execute(null);

    public void OnDisappearing()
    {
        CameraState = CameraState.Off;
        IsTyping = false;
    }

    // Restores the draft once, the first time the page appears (not after returning from another page), reads the
    // catalog for the duplicate warning and starts the camera while there is no barcode yet
    [RelayCommand]
    private async Task AppearAsync()
    {
        if (!_opened)
        {
            _opened = true;
            if (!_prefilled && _editing is null && appState.GetDraft<ProductFormDraft>() is { } draft)
            {
                Name = draft.Name;
                Barcode = draft.Barcode;
                Photo.Restore(draft.PhotoPath);
            }
        }

        await QuickSearch.LoadCommand.ExecuteAsync(null);
        UpdateDuplicate();
        if (_editing is null && Barcode.Length == 0)
        {
            await StartCameraAsync();
        }
    }

    // The "Scan barcode" and "Allow camera" buttons
    [RelayCommand]
    private Task StartScanAsync() => StartCameraAsync();

    // The camera reports the same code again and again: the first valid read closes it
    [RelayCommand]
    private void ScanDetected(string? text)
    {
        if (CameraState != CameraState.Active)
        {
            return;
        }

        var parsed = ScanPayloadParser.Parse(text);
        if (!parsed.IsSuccess)
        {
            StatusMessage = parsed.Error!;
            return;
        }

        CameraState = CameraState.Off;
        StatusMessage = string.Empty;
        Barcode = parsed.Value!;
    }

    // Asks Open Food Facts about a new, valid barcode and fills the name if it is still empty. Offline or unknown only
    // shows a message. A barcode that is already in the catalog is not asked about
    [RelayCommand]
    private async Task LookupAsync()
    {
        var barcode = Barcode;
        if (_editing is not null || barcode == _lookedUp || Name.Length > 0
            || currentUser.CurrentUser is not { } user || !RolePermissions.CanManageProducts(user.Role)
            || validator.ValidateBarcodeFormat(barcode) is not null
            || QuickSearch.Products.Any(product => product.Barcode == barcode))
        {
            return;
        }

        _lookedUp = barcode;
        var result = await lookup.FindAsync(barcode);

        // The user went on while the answer was on its way
        if (Barcode != barcode)
        {
            return;
        }

        if (!result.IsFound)
        {
            LookupText = LookupMessages.Failure(result.Status, barcode);
            logger.LogWarning("Lookup of barcode {Barcode} gave {Status}", barcode, result.Status);
            return;
        }

        if (Name.Length == 0)
        {
            Name = result.Name;
            DataSource = lookup.Attribution;
            LookupText = "Found in Open Food Facts, name filled in.";
        }
    }

    [RelayCommand]
    private Task OpenDuplicateAsync() =>
        Duplicate is null
            ? Task.CompletedTask
            : navigation.GoToAsync(Routes.ProductDetail,
                new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = Duplicate });

    private async Task StartCameraAsync()
    {
        if (!scanner.IsCameraSupported)
        {
            CameraState = CameraState.Unsupported;
        }
        else if (await scanner.RequestCameraAccessAsync())
        {
            CameraState = CameraState.Active;
        }
        else
        {
            CameraState = CameraState.Denied;
            logger.LogWarning("Camera access was denied, the barcode must be typed");
        }
    }

    // The product the user tapped, or one with exactly this barcode or name (never the one being edited)
    private void UpdateDuplicate()
    {
        var name = Name.Trim();
        var barcode = Barcode.Trim();
        Duplicate = _picked
                    ?? QuickSearch.Products.FirstOrDefault(product => product.Id != _editing?.Id
                        && (product.Barcode == barcode
                            || string.Equals(product.Name, name, StringComparison.OrdinalIgnoreCase)));
        DuplicateText = Duplicate is null ? string.Empty : $"Already in the catalog: {Duplicate.Name} ({Duplicate.Barcode})";
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (await guard.CheckAsync(RolePermissions.CanManageProducts, _editing is null ? "add products" : "edit products") is { } refusal)
        {
            StatusMessage = refusal;
            return;
        }

        // Uniqueness needs the repository, so it is checked on save, not on every keystroke.
        // When editing, the product's own barcode is not a duplicate
        var barcodeError = await validator.ValidateBarcodeAsync(Barcode, _editing?.Id);
        if (barcodeError is not null)
        {
            BarcodeError = barcodeError;
            logger.LogWarning("Product form rejected barcode {Barcode}: {Reason}", Barcode, barcodeError);
            return;
        }

        var result = _editing is null
            ? await productService.AddAsync(Name, Barcode, Photo.Path)
            : await productService.UpdateAsync(_editing.Id, Name, Barcode, Photo.Path);
        if (!result.IsSuccess)
        {
            StatusMessage = result.Error!;
            logger.LogWarning("Product form was not saved: {Reason}", result.Error);
            return;
        }

        logger.LogDebug("Product form saved product {ProductId} ({Mode})", result.Value!.Id, _editing is null ? "add" : "edit");
        Photo.Commit();
        if (_editing is null)
        {
            appState.ClearDraft<ProductFormDraft>();
        }

        // The list updates the row from these parameters instead of reloading. An edited row keeps its
        // quantity and characteristic values
        var product = result.Value!;
        var summary = _editing is null
            ? new ProductSummary(product.Id, product.Name, product.Barcode, 0) { PhotoPath = product.PhotoPath }
            : _editing with { Name = product.Name, Barcode = product.Barcode, PhotoPath = product.PhotoPath };
        await navigation.GoToAsync(Routes.Back, new Dictionary<string, object>
        {
            [NavigationKeys.UpdatedProduct] = summary
        });
    }

    [RelayCommand]
    private async Task TakePhotoAsync() => await ChangePhotoAsync(Photo.TakeAsync);

    [RelayCommand]
    private async Task PickPhotoAsync() => await ChangePhotoAsync(Photo.PickAsync);

    [RelayCommand]
    private async Task RemovePhotoAsync()
    {
        StatusMessage = await Photo.RemoveAsync();
        SaveDraft();
    }

    // The photo file is named after the barcode, so the barcode must be valid first
    private async Task ChangePhotoAsync(Func<string, Task<string>> change)
    {
        if (validator.ValidateBarcodeFormat(Barcode) is not null)
        {
            StatusMessage = "Enter a valid barcode first: the photo file is named after it.";
            logger.LogWarning("Photo not added: the barcode {Barcode} is not valid yet", Barcode);
            return;
        }

        StatusMessage = await change(Barcode.Trim());
        SaveDraft();
    }

    [RelayCommand]
    private Task CancelAsync()
    {
        Photo.Discard();
        if (_editing is null)
        {
            appState.ClearDraft<ProductFormDraft>();
        }

        return navigation.GoToAsync(Routes.Back);
    }

    private bool CanSave() =>
        currentUser.CurrentUser is { } user
        && RolePermissions.CanManageProducts(user.Role)
        && validator.ValidateName(Name) is null
        && validator.ValidateBarcodeFormat(Barcode) is null;

    private void SaveDraft()
    {
        if (_editing is not null)
        {
            return;
        }

        if (Name.Length == 0 && Barcode.Length == 0 && Photo.Path is null)
        {
            appState.ClearDraft<ProductFormDraft>();
        }
        else
        {
            appState.SetDraft(new ProductFormDraft(Name, Barcode, Photo.Path));
        }
    }
}
