using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Add/edit-product form. Opened from the list (add, or edit with the product in the navigation parameters),
// or from the scanner with an unknown barcode (prefilled).
// A new product's typed text is kept as a draft in IAppStateService until it is saved or cancelled;
// editing keeps no draft
public partial class ProductFormViewModel(
    IProductValidator validator,
    IProductService productService,
    ICurrentUserService currentUser,
    IPermissionGuard guard,
    IAppStateService appState,
    INavigationService navigation,
    ProductPhotoEditor photo,
    ILogger<ProductFormViewModel> logger) : ObservableObject, IQueryAttributable, IPageLifecycleAware
{
    private bool _opened;
    private bool _prefilled;

    // The product being edited; null when adding
    private ProductSummary? _editing;

    public string PageTitle => _editing is null ? "New product" : "Edit product";

    // The photo of the product (staged until Save, see ProductPhotoEditor)
    public ProductPhotoEditor Photo { get; } = photo;

    // Photo buttons are for the Manager (the form is too, but the check is made here as well)
    public bool CanManagePhotos => currentUser.CurrentUser is { } user && RolePermissions.CanManagePhotos(user.Role);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
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
        SaveDraft();
    }

    partial void OnBarcodeChanged(string value)
    {
        BarcodeError = validator.ValidateBarcodeFormat(value) ?? string.Empty;
        SaveDraft();
    }

    // The scanner passes the unknown barcode it could not find; the list passes the product to edit
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationKeys.SelectedProduct, out var selected) && selected is ProductSummary product)
        {
            _editing = product;
            OnPropertyChanged(nameof(PageTitle));
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

    // Restores the draft once, the first time the page appears (not after returning from another page)
    public void OnAppearing()
    {
        if (_opened)
        {
            return;
        }

        _opened = true;
        if (_prefilled || _editing is not null || appState.GetDraft<ProductFormDraft>() is not { } draft)
        {
            return;
        }

        Name = draft.Name;
        Barcode = draft.Barcode;
        Photo.Restore(draft.PhotoPath);
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
