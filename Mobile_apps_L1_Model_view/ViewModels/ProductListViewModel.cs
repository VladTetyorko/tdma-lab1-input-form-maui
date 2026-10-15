using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Receives changed products from the pages it opens (IQueryAttributable), so it never has to reload
public partial class ProductListViewModel(
    IProductCatalog catalog,
    ICharacteristicService characteristics,
    ICurrentUserService currentUser,
    INavigationService navigation) : ObservableObject, IQueryAttributable, IPageLifecycleAware
{
    private List<ProductSummary> _all = [];
    private Guid? _loadedForUserId;
    private bool _sortOptionsStale;

    // What the list shows: _all, sorted
    public ObservableCollection<ProductSummary> Products { get; } = [];

    // Only ever grows (characteristics are never deleted), so the picker keeps its selection
    public ObservableCollection<SortOption> SortOptions { get; } = [SortOption.ByName, SortOption.ByQuantity];

    [ObservableProperty]
    public partial SortOption? SelectedSortOption { get; set; } = SortOption.ByName;

    [ObservableProperty]
    public partial bool SortDescending { get; set; }

    [ObservableProperty]
    public partial string CurrentUserName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    public partial bool CanAddProduct { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCharacteristicsCommand))]
    public partial bool CanManageCharacteristics { get; set; }

    partial void OnSelectedSortOptionChanged(SortOption? value) => ApplySort();

    partial void OnSortDescendingChanged(bool value) => ApplySort();

    public void OnAppearing() => EnsureLoadedCommand.Execute(null);

    // Called by the page every time it appears: loads once per signed-in user.
    // Changes made on other pages arrive through ApplyQueryAttributes instead
    [RelayCommand]
    private async Task EnsureLoadedAsync()
    {
        if (_loadedForUserId is not null && _loadedForUserId == currentUser.CurrentUser?.Id)
        {
            if (_sortOptionsStale)
            {
                await SyncSortOptionsAsync();
            }

            return;
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var user = currentUser.CurrentUser;
        CurrentUserName = user is null ? string.Empty : $"{user.FullName} ({user.Role})";
        CanAddProduct = user is not null && RolePermissions.CanManageProducts(user.Role);
        CanManageCharacteristics = CanAddProduct;

        _all = [.. await catalog.GetSummariesAsync()];
        await SyncSortOptionsAsync();
        _loadedForUserId = user?.Id;
        ApplySort();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationKeys.UpdatedProduct, out var value) && value is ProductSummary updated)
        {
            var index = _all.FindIndex(p => p.Id == updated.Id);
            if (index < 0)
            {
                _all.Add(updated);
            }
            else
            {
                _all[index] = updated;
            }

            ApplySort();
        }

        if (query.ContainsKey(NavigationKeys.CharacteristicsChanged))
        {
            _sortOptionsStale = true;
        }
    }

    [RelayCommand]
    private Task OpenDetailsAsync(ProductSummary? product) =>
        product is null
            ? Task.CompletedTask
            : navigation.GoToAsync(Routes.ProductDetail,
                new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = product });

    [RelayCommand(CanExecute = nameof(CanAddProduct))]
    private Task AddProductAsync() => navigation.GoToAsync(Routes.ProductForm);

    [RelayCommand(CanExecute = nameof(CanManageCharacteristics))]
    private Task OpenCharacteristicsAsync() => navigation.GoToAsync(Routes.Characteristics);

    [RelayCommand]
    private Task SignOutAsync()
    {
        currentUser.SignOut();
        return navigation.GoToAsync(Routes.Absolute(Routes.Login));
    }

    private async Task SyncSortOptionsAsync()
    {
        foreach (var option in SortOption.All(await characteristics.GetAllAsync()))
        {
            if (!SortOptions.Contains(option))
            {
                SortOptions.Add(option);
            }
        }

        _sortOptionsStale = false;
    }

    private void ApplySort()
    {
        if (SelectedSortOption is null)
        {
            return;
        }

        Products.Clear();
        foreach (var product in ProductSorter.Sort(_all, SelectedSortOption, SortDescending))
        {
            Products.Add(product);
        }
    }
}
