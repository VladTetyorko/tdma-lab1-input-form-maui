using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Receives changed products from the pages it opens (IQueryAttributable), so it never has to reload
public partial class ProductListViewModel(
    IProductCatalog catalog,
    ICharacteristicService characteristics,
    IProductService productService,
    ICurrentUserService currentUser,
    ISessionStore session,
    IAppStateService appState,
    IStartupNavigator startup,
    IAlertService alerts,
    IPermissionGuard guard,
    INavigationService navigation,
    IShakeGesture shake,
    ConnectivityBannerViewModel connectivity,
    ILogger<ProductListViewModel> logger) : ObservableObject, IQueryAttributable, IPageLifecycleAware
{
    private List<ProductSummary> _all = [];
    private Guid? _loadedForUserId;
    private bool _sortOptionsStale;
    private bool _productsStale;

    // Offline / syncing / waiting changes, shown above the list
    public ConnectivityBannerViewModel Connectivity { get; } = connectivity;

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

    // Edit and delete buttons on the rows (Manager)
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditProductCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteProductCommand))]
    public partial bool CanManageProducts { get; set; }

    // Why a delete was refused, or empty
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCharacteristicsCommand))]
    public partial bool CanManageCharacteristics { get; set; }

    partial void OnSelectedSortOptionChanged(SortOption? value) => ApplySort();

    partial void OnSortDescendingChanged(bool value) => ApplySort();

    // The shake sensor and the banner listen only while the page is visible. Shake opens the scanner ("quick scan")
    public void OnAppearing()
    {
        EnsureLoadedCommand.Execute(null);
        shake.Start(() => navigation.GoToAsync(Routes.Scanner));
        Connectivity.Attach();
    }

    public void OnDisappearing()
    {
        shake.Stop();
        Connectivity.Detach();
    }

    // Called by the page every time it appears: loads once per signed-in user.
    // Changes made on other pages arrive through ApplyQueryAttributes instead
    [RelayCommand]
    private async Task EnsureLoadedAsync()
    {
        if (_loadedForUserId is not null && _loadedForUserId == currentUser.CurrentUser?.Id && !_productsStale)
        {
            if (_sortOptionsStale)
            {
                await SyncSortOptionsAsync();
            }
        }
        else
        {
            await LoadAsync();
        }

        // A deep link or an unfinished form from the last run (does nothing when there is none)
        await startup.RunAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var user = currentUser.CurrentUser;
        CurrentUserName = user is null ? string.Empty : $"{user.FullName} ({user.Role})";
        CanAddProduct = user is not null && RolePermissions.CanManageProducts(user.Role);
        CanManageCharacteristics = CanAddProduct;
        CanManageProducts = CanAddProduct;
        StatusMessage = string.Empty;

        _all = [.. await catalog.GetSummariesAsync()];
        await SyncSortOptionsAsync();
        _loadedForUserId = user?.Id;
        _productsStale = false;
        ApplySort();
        logger.LogInformation("Product list loaded: {Count} products for user {UserId}", _all.Count, user?.Id);
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

        if (query.ContainsKey(NavigationKeys.ProductsChanged))
        {
            _productsStale = true;
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
    private async Task AddProductAsync()
    {
        if (await RefusedAsync("add products"))
        {
            return;
        }

        await navigation.GoToAsync(Routes.ProductForm);
    }

    [RelayCommand(CanExecute = nameof(CanManageProducts))]
    private async Task EditProductAsync(ProductSummary? product)
    {
        if (product is null || await RefusedAsync("edit products"))
        {
            return;
        }

        await navigation.GoToAsync(Routes.ProductForm,
            new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = product });
    }

    // Only a product without operations can go: the history of In/Out must stay complete
    [RelayCommand(CanExecute = nameof(CanManageProducts))]
    private async Task DeleteProductAsync(ProductSummary? product)
    {
        StatusMessage = string.Empty;
        if (product is null || await RefusedAsync("delete products"))
        {
            return;
        }

        // A product with history is not worth a question: the service refuses it and says why
        if (product.CanDelete
            && !await alerts.ConfirmAsync("Delete product", $"Delete \"{product.Name}\"?", "Delete", "Cancel"))
        {
            return;
        }

        var result = await productService.DeleteAsync(product.Id);
        if (!result.IsSuccess)
        {
            StatusMessage = result.Error!;
            logger.LogWarning("Delete of product {ProductId} was refused", product.Id);
            return;
        }

        logger.LogDebug("Product {ProductId} removed from the list", product.Id);
        _all.RemoveAll(p => p.Id == product.Id);
        ApplySort();
    }

    [RelayCommand(CanExecute = nameof(CanManageCharacteristics))]
    private async Task OpenCharacteristicsAsync()
    {
        if (await RefusedAsync("manage characteristics"))
        {
            return;
        }

        await navigation.GoToAsync(Routes.Characteristics);
    }

    [RelayCommand]
    private Task OpenSettingsAsync() => navigation.GoToAsync(Routes.Settings);

    [RelayCommand]
    private Task ScanAsync() => navigation.GoToAsync(Routes.Scanner);

    [RelayCommand]
    private Task OpenMapAsync() => navigation.GoToAsync(Routes.Map);

    [RelayCommand]
    private async Task SignOutAsync()
    {
        logger.LogInformation("User {UserId} signed out", currentUser.CurrentUser?.Id);
        currentUser.SignOut();
        try
        {
            await session.ClearAsync();
        }
        catch (Exception ex)
        {
            // Signing out must work even when secure storage does not: the user is signed out in memory anyway
            logger.LogWarning(ex, "The saved session could not be removed");
        }

        appState.Clear();
        await navigation.GoToAsync(Routes.Absolute(Routes.Login));
    }

    // The role check (the alert and the warning log are the guard's); the reason also goes to the status line
    private async Task<bool> RefusedAsync(string action)
    {
        var refusal = await guard.CheckAsync(RolePermissions.CanManageProducts, action);
        if (refusal is not null)
        {
            StatusMessage = refusal;
        }

        return refusal is not null;
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
