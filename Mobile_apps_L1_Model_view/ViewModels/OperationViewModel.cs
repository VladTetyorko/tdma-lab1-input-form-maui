using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Records "N units of this product moved In or Out". Opened from the detail page and from the
// barcode scanner (both pass the product in the navigation parameters). What is typed is kept as a
// draft in IOperationDraftStore (counterparty encrypted) until the operation is saved or the page is
// left with Cancel/back.
public partial class OperationViewModel(
    IStockService stock,
    ICurrentUserService currentUser,
    IOperationDraftStore drafts,
    INavigationService navigation) : ObservableObject, IQueryAttributable, IPageLifecycleAware
{
    private Guid _productId;
    private string _barcode = string.Empty;
    private Task _restoring = Task.CompletedTask;

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

    partial void OnSelectedDirectionChanged(OperationDirection value) => SaveDraft();

    partial void OnQuantityChanged(int value) => SaveDraft();

    partial void OnCounterpartyChanged(string value) => SaveDraft();

    partial void OnReasonChanged(string value) => SaveDraft();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationKeys.SelectedProduct, out var value) && value is ProductSummary product)
        {
            _productId = product.Id;
            _barcode = product.Barcode;
            ProductName = product.Name;
            QuantityOnHand = product.Quantity;
            _restoring = RestoreDraftAsync();
        }
    }

    public void OnAppearing() => LoadCommand.Execute(null);

    [RelayCommand]
    private async Task LoadAsync()
    {
        await _restoring;
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

        drafts.Clear();

        // The detail page reloads when it appears again
        await navigation.GoToAsync(Routes.Back);
    }

    [RelayCommand]
    private Task GoBackAsync()
    {
        drafts.Clear();
        return navigation.GoToAsync(Routes.Back);
    }

    // Only a draft for this very product applies; the local copy survives the property setters below,
    // which each save a draft of their own
    private async Task RestoreDraftAsync()
    {
        if (await drafts.LoadAsync(_barcode) is not { } draft)
        {
            return;
        }

        SelectedDirection = draft.Direction;
        Quantity = draft.Quantity;
        Counterparty = draft.Counterparty;
        Reason = draft.Reason;
    }

    private void SaveDraft()
    {
        if (_barcode.Length > 0)
        {
            _ = drafts.SaveAsync(new OperationDraft(_barcode, SelectedDirection, Quantity, Counterparty, Reason));
        }
    }

    private static string? Blank(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
