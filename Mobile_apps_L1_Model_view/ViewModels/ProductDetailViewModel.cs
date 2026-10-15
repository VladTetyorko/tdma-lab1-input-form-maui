using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Opened from the list with the selected ProductSummary in the navigation parameters
public partial class ProductDetailViewModel(
    IProductCatalog catalog,
    ICharacteristicService characteristics,
    ICharacteristicValidator validator,
    ICurrentUserService currentUser,
    INavigationService navigation) : ObservableObject, IQueryAttributable, IPageLifecycleAware
{
    private ProductSummary? _product;

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

        foreach (var item in Characteristics)
        {
            item.PropertyChanged -= OnItemChanged;
        }

        Characteristics.Clear();
        foreach (var source in details.Characteristics)
        {
            var item = new CharacteristicEditItem(source, validator, CanEdit);
            item.PropertyChanged += OnItemChanged;
            Characteristics.Add(item);
        }

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

        await ReturnToListAsync();
    }

    [RelayCommand]
    private Task GoBackAsync() => ReturnToListAsync();

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

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e) => SaveCommand.NotifyCanExecuteChanged();
}
