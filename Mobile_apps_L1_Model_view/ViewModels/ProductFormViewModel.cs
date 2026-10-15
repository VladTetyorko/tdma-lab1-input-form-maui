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
    IAppStateService appState,
    INavigationService navigation) : ObservableObject, IQueryAttributable, IPageLifecycleAware
{
    private bool _opened;
    private bool _prefilled;

    // The product being edited; null when adding
    private ProductSummary? _editing;

    public string PageTitle => _editing is null ? "New product" : "Edit product";

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
        }

        if (query.TryGetValue(NavigationKeys.Barcode, out var value) && value is string barcode)
        {
            _prefilled = true;
            Barcode = barcode;
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
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        // Uniqueness needs the repository, so it is checked on save, not on every keystroke.
        // When editing, the product's own barcode is not a duplicate
        var barcodeError = await validator.ValidateBarcodeAsync(Barcode, _editing?.Id);
        if (barcodeError is not null)
        {
            BarcodeError = barcodeError;
            return;
        }

        var result = _editing is null
            ? await productService.AddAsync(Name, Barcode)
            : await productService.UpdateAsync(_editing.Id, Name, Barcode);
        if (!result.IsSuccess)
        {
            StatusMessage = result.Error!;
            return;
        }

        if (_editing is null)
        {
            appState.ClearDraft<ProductFormDraft>();
        }

        // The list updates the row from these parameters instead of reloading. An edited row keeps its
        // quantity and characteristic values
        var product = result.Value!;
        var summary = _editing is null
            ? new ProductSummary(product.Id, product.Name, product.Barcode, 0)
            : _editing with { Name = product.Name, Barcode = product.Barcode };
        await navigation.GoToAsync(Routes.Back, new Dictionary<string, object>
        {
            [NavigationKeys.UpdatedProduct] = summary
        });
    }

    [RelayCommand]
    private Task CancelAsync()
    {
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

        if (Name.Length == 0 && Barcode.Length == 0)
        {
            appState.ClearDraft<ProductFormDraft>();
        }
        else
        {
            appState.SetDraft(new ProductFormDraft(Name, Barcode));
        }
    }
}
