using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

public partial class ProductFormViewModel(
    IProductValidator validator,
    IProductService productService,
    ICurrentUserService currentUser,
    INavigationService navigation) : ObservableObject
{
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

    partial void OnNameChanged(string value) => NameError = validator.ValidateName(value) ?? string.Empty;

    partial void OnBarcodeChanged(string value) =>
        BarcodeError = validator.ValidateBarcodeFormat(value) ?? string.Empty;

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

        // The list adds the new product from these parameters instead of reloading
        var product = result.Value!;
        await navigation.GoToAsync(Routes.Back, new Dictionary<string, object>
        {
            [NavigationKeys.UpdatedProduct] = new ProductSummary(product.Id, product.Name, product.Barcode, 0)
        });
    }

    [RelayCommand]
    private Task CancelAsync() => navigation.GoToAsync(Routes.Back);

    private bool CanSave() =>
        currentUser.CurrentUser is { } user
        && RolePermissions.CanManageProducts(user.Role)
        && validator.ValidateName(Name) is null
        && validator.ValidateBarcodeFormat(Barcode) is null;
}
