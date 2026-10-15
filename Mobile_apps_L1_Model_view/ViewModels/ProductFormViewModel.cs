using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Add-product form. Opened from the list, or from the scanner with an unknown barcode (prefilled).
// What is typed is kept as a draft in IAppStateService until the product is saved or the form cancelled
public partial class ProductFormViewModel(
    IProductValidator validator,
    IProductService productService,
    ICurrentUserService currentUser,
    IAppStateService appState,
    INavigationService navigation) : ObservableObject, IQueryAttributable, IPageLifecycleAware
{
    private bool _opened;
    private bool _prefilled;

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

    // The scanner passes the unknown barcode it could not find
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
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
        if (_prefilled || appState.GetDraft<ProductFormDraft>() is not { } draft)
        {
            return;
        }

        Name = draft.Name;
        Barcode = draft.Barcode;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        // Uniqueness needs the repository, so it is checked on save, not on every keystroke
        var barcodeError = await validator.ValidateBarcodeAsync(Barcode);
        if (barcodeError is not null)
        {
            BarcodeError = barcodeError;
            return;
        }

        var result = await productService.AddAsync(Name, Barcode);
        if (!result.IsSuccess)
        {
            StatusMessage = result.Error!;
            return;
        }

        appState.ClearDraft<ProductFormDraft>();

        // The list adds the new product from these parameters instead of reloading
        var product = result.Value!;
        await navigation.GoToAsync(Routes.Back, new Dictionary<string, object>
        {
            [NavigationKeys.UpdatedProduct] = new ProductSummary(product.Id, product.Name, product.Barcode, 0)
        });
    }

    [RelayCommand]
    private Task CancelAsync()
    {
        appState.ClearDraft<ProductFormDraft>();
        return navigation.GoToAsync(Routes.Back);
    }

    private bool CanSave() =>
        currentUser.CurrentUser is { } user
        && RolePermissions.CanManageProducts(user.Role)
        && validator.ValidateName(Name) is null
        && validator.ValidateBarcodeFormat(Barcode) is null;

    private void SaveDraft()
    {
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
