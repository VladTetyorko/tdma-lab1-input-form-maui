using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Opened from the list with the selected ProductSummary in the navigation parameters.
// Characteristic edits that are not saved yet are kept as a draft in IAppStateService, so they
// survive the page reloading when it appears again (after the operation page) and the app being stopped.
public partial class ProductDetailViewModel(
    IProductCatalog catalog,
    ICharacteristicService characteristics,
    ICharacteristicValidator validator,
    ICurrentUserService currentUser,
    IAppStateService appState,
    INavigationService navigation) : ObservableObject, IQueryAttributable, IPageLifecycleAware
{
    private ProductSummary? _product;

    // Stored value of each characteristic as last loaded: an edit is "unsaved" when it differs
    private Dictionary<Guid, string> _stored = [];

    // True while items are being built or the draft applied (those assignments are not user edits)
    private bool _rebuilding;

    public ObservableCollection<CharacteristicEditItem> Characteristics { get; } = [];

    // Newest first
    public ObservableCollection<OperationHistoryItem> History { get; } = [];

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Barcode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int Quantity { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool CanEdit { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationKeys.SelectedProduct, out var value) && value is ProductSummary product)
        {
            _product = product;
            ShowSummary(product);
        }
    }

    public void OnAppearing() => LoadCommand.Execute(null);

    // Everything else is read from the services every time the page appears,
    // so it also refreshes after the operation page records something
    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_product is null)
        {
            return;
        }

        var details = await catalog.GetDetailsAsync(_product.Id);
        if (details is null)
        {
            StatusMessage = "This product no longer exists.";
            return;
        }

        var user = currentUser.CurrentUser;
        CanEdit = user is not null && RolePermissions.CanManageProducts(user.Role);
        _product = details.Summary;
        ShowSummary(details.Summary);

        _rebuilding = true;
        RebuildCharacteristics(details.Characteristics);
        RestoreDraft();
        _rebuilding = false;

        History.Clear();
        foreach (var line in details.History)
        {
            History.Add(line);
        }

        StatusMessage = string.Empty;
        SaveCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var values = Characteristics.ToDictionary(c => c.CharacteristicId, c => (string?)c.Value);
        var result = await characteristics.SetValuesAsync(_product!.Id, values);
        if (!result.IsSuccess)
        {
            StatusMessage = result.Error!;
            return;
        }

        appState.ClearDraft<ProductDetailDraft>();
        await ReturnToListAsync();
    }

    [RelayCommand]
    private Task GoBackAsync()
    {
        appState.ClearDraft<ProductDetailDraft>();
        return ReturnToListAsync();
    }

    // The draft stays: the edits are still on screen when this page appears again
    [RelayCommand]
    private Task OpenOperationAsync() =>
        _product is null
            ? Task.CompletedTask
            : navigation.GoToAsync(Routes.Operation,
                new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = _product });

    private bool CanSave() => CanEdit && _product is not null && Characteristics.All(c => c.Error.Length == 0);

    // Sends the product, as it is now, back so the list can update its row without reloading
    private async Task ReturnToListAsync()
    {
        var current = _product is null ? null : await catalog.GetSummaryAsync(_product.Id);
        if (current is null)
        {
            await navigation.GoToAsync(Routes.Back);
            return;
        }

        await navigation.GoToAsync(Routes.Back,
            new Dictionary<string, object> { [NavigationKeys.UpdatedProduct] = current });
    }

    private void ShowSummary(ProductSummary product)
    {
        Name = product.Name;
        Barcode = product.Barcode;
        Quantity = product.Quantity;
    }

    private void RebuildCharacteristics(IReadOnlyList<CharacteristicValueItem> sources)
    {
        foreach (var item in Characteristics)
        {
            item.PropertyChanged -= OnItemChanged;
        }

        Characteristics.Clear();
        foreach (var source in sources)
        {
            var item = new CharacteristicEditItem(source, validator, CanEdit);
            item.PropertyChanged += OnItemChanged;
            Characteristics.Add(item);
        }

        _stored = sources.ToDictionary(s => s.CharacteristicId, s => s.Value);
    }

    // Puts the unsaved edits of this product (if any) back into the freshly built items
    private void RestoreDraft()
    {
        if (!CanEdit || appState.GetDraft<ProductDetailDraft>() is not { } draft || draft.Barcode != _product!.Barcode)
        {
            return;
        }

        foreach (var item in Characteristics.Where(i => draft.Values.ContainsKey(i.CharacteristicId)))
        {
            item.Value = draft.Values[item.CharacteristicId];
        }
    }

    // The draft is the set of values that differ from the stored ones; none = no draft
    private void SaveDraft()
    {
        var edits = Characteristics
            .Where(c => c.Value != _stored.GetValueOrDefault(c.CharacteristicId, string.Empty))
            .ToDictionary(c => c.CharacteristicId, c => c.Value);

        if (edits.Count == 0)
        {
            appState.ClearDraft<ProductDetailDraft>();
        }
        else
        {
            appState.SetDraft(new ProductDetailDraft(_product!.Barcode, edits));
        }
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        SaveCommand.NotifyCanExecuteChanged();
        if (e.PropertyName == nameof(CharacteristicEditItem.Value) && !_rebuilding && CanEdit)
        {
            SaveDraft();
        }
    }
}
