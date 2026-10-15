using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Records "N units of this product moved In or Out". Opened from the detail page now,
// from the barcode scanner in Lab 3 (both pass the product in the navigation parameters).
public partial class OperationViewModel(
    IStockService stock,
    ICurrentUserService currentUser,
    INavigationService navigation) : ObservableObject, IQueryAttributable
{
    private Guid _productId;

    // Only the directions the current role may record (a Driver can only bring goods In)
    public IReadOnlyList<OperationDirection> AvailableDirections { get; private set; } = [];

    [ObservableProperty]
    public partial string ProductName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int QuantityOnHand { get; set; }

    [ObservableProperty]
    public partial OperationDirection SelectedDirection { get; set; } = OperationDirection.In;

    [ObservableProperty]
    public partial int Quantity { get; set; } = 1;

    [ObservableProperty]
    public partial string Counterparty { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Reason { get; set; } = string.Empty;

    // Empty string = no error (keeps XAML bindings simple)
    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationKeys.SelectedProduct, out var value) && value is ProductSummary product)
        {
            _productId = product.Id;
            ProductName = product.Name;
            QuantityOnHand = product.Quantity;
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var user = currentUser.CurrentUser;
        AvailableDirections = user is null
            ? []
            : Enum.GetValues<OperationDirection>().Where(d => RolePermissions.CanRecord(user.Role, d)).ToList();
        OnPropertyChanged(nameof(AvailableDirections));
        if (!AvailableDirections.Contains(SelectedDirection) && AvailableDirections.Count > 0)
        {
            SelectedDirection = AvailableDirections[0];
        }

        if (_productId != Guid.Empty)
        {
            QuantityOnHand = await stock.GetQuantityOnHandAsync(_productId);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var result = await stock.RecordAsync(
            _productId, SelectedDirection, Quantity, Blank(Counterparty), Blank(Reason));
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error!;
            return;
        }

        // The detail page reloads when it appears again
        await navigation.GoToAsync(Routes.Back);
    }

    [RelayCommand]
    private Task GoBackAsync() => navigation.GoToAsync(Routes.Back);

    private static string? Blank(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
