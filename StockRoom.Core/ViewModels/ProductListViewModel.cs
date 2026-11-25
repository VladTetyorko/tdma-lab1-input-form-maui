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
    IStartupNavigator startup,
    IAlertService alerts,
    IPermissionGuard guard,
    INavigationService navigation,
    IShakeGesture shake,
    ILogger<ProductListViewModel> logger,
    StockChanges? stockChanges = null) : ObservableObject, INavigationParameterReceiver, IPageLifecycleAware
{
    private List<ProductSummary> _all = [];
    private Dictionary<Guid, Services.Search.MatchResult> _matches = [];
    private Guid? _loadedForUserId;
    private bool _sortOptionsStale;
    private bool _productsStale;
    private int _stockVersion;
    private bool _loaded;

    // What the list shows: _all, filtered by SearchText, then sorted
    public ObservableCollection<ProductSummary> Products { get; } = [];

    // Only ever grows (characteristics are never deleted), so the picker keeps its selection
    public ObservableCollection<SortOption> SortOptions { get; } = [SortOption.ByName, SortOption.ByQuantity];

    // The table: the stretching name column, the other headers and the rows in header order
    [ObservableProperty]
    public partial TableColumn NameColumn { get; set; } = ProductTable.NameColumn(SortOption.ByName, false);

    public ObservableCollection<TableColumn> Columns { get; } = [];

    public ObservableCollection<ProductTableRow> Rows { get; } = [];

    // Characteristics a narrow (phone) table can show as its one extra column
    public ObservableCollection<SortOption> ExtraColumnOptions { get; } = [];

    [ObservableProperty]
    public partial SortOption? SelectedExtraColumn { get; set; }

    // Set by the page from its width
    [ObservableProperty]
    public partial bool IsNarrow { get; set; }

    [ObservableProperty]
    public partial SortOption? SelectedSortOption { get; set; } = SortOption.ByName;

    [ObservableProperty]
    public partial bool SortDescending { get; set; }

    // Not persisted: a search is a quick look, not a draft (the list starts unfiltered after a restart)
    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    // Shown instead of the list when there is nothing in it: no match, or no product at all
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyShown))]
    public partial string EmptyText { get; set; } = string.Empty;

    // What to do about it
    [ObservableProperty]
    public partial string EmptyHint { get; set; } = string.Empty;

    // "Clear search" when the search is the reason, else empty
    [ObservableProperty]
    public partial string EmptyActionText { get; set; } = string.Empty;

    public bool IsEmptyShown => EmptyText.Length > 0;

    // Pull to refresh
    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

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

    partial void OnSearchTextChanged(string value) => ApplySort();

    partial void OnSelectedExtraColumnChanged(SortOption? value) => ApplySort();

    partial void OnIsNarrowChanged(bool value) => ApplySort();

    // A tap on a column header: the same column flips the direction, another one starts ascending
    [RelayCommand]
    private void SortByColumn(TableColumn? column)
    {
        if (column?.Sort is not { } clicked || SelectedSortOption is null)
        {
            return;
        }

        var (sort, descending) = ProductTable.Toggle(SelectedSortOption, SortDescending, clicked);
        SortDescending = descending;
        SelectedSortOption = sort;
    }

    // The shake sensor listens only while the page is visible. Shake opens the scanner ("quick scan")
    public void OnAppearing()
    {
        EnsureLoadedCommand.Execute(null);
        shake.Start(() => navigation.GoToAsync(Routes.Scanner));
    }

    public void OnDisappearing()
    {
        shake.Stop();
    }

    // Called by the page every time it appears: loads once per signed-in user.
    // Changes made on other pages arrive through ApplyQueryAttributes instead
    [RelayCommand]
    private async Task EnsureLoadedAsync()
    {
        if (_loadedForUserId is not null && _loadedForUserId == currentUser.CurrentUser?.Id && !_productsStale
            && _stockVersion == (stockChanges?.Version ?? 0))
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

        _stockVersion = stockChanges?.Version ?? 0;
        _all = [.. await catalog.GetSummariesAsync()];
        _loaded = true;
        await SyncSortOptionsAsync();
        _loadedForUserId = user?.Id;
        _productsStale = false;
        ApplySort();
        logger.LogInformation("Product list loaded: {Count} products for user {UserId}", _all.Count, user?.Id);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            await LoadAsync();
        }
        catch (Exception ex)
        {
            // The list keeps what it showed
            logger.LogWarning(ex, "The product list could not be refreshed");
            StatusMessage = "The products could not be read. Try again.";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

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
                if (option.Kind == SortKind.Characteristic)
                {
                    ExtraColumnOptions.Add(option);
                }
            }
        }

        _sortOptionsStale = false;
        SelectedExtraColumn ??= ExtraColumnOptions.FirstOrDefault();
        ApplySort();
    }

    private void ApplySort()
    {
        if (SelectedSortOption is null)
        {
            return;
        }

        Products.Clear();
        var matches = ProductSearch.Filter(_all, SearchText);
        _matches = matches.ToDictionary(match => match.Product.Id);
        foreach (var product in ProductSorter.Sort(matches.Select(match => match.Product), SelectedSortOption,
                     SortDescending))
        {
            Products.Add(product);
        }

        ApplyTable(SelectedSortOption);

        ApplyEmptyState();
    }

    private void ApplyEmptyState()
    {
        var noMatch = Products.Count == 0 && _all.Count > 0;
        var noProducts = _loaded && _all.Count == 0;
        EmptyText = noMatch ? $"No products match \"{SearchText.Trim()}\"."
            : noProducts ? "No products yet."
            : string.Empty;
        EmptyHint = !noProducts ? string.Empty
            : currentUser.CurrentUser is { } user && RolePermissions.CanManageProducts(user.Role)
                ? "Add the first product."
                : "A manager adds them.";
        EmptyActionText = noMatch ? "Clear search" : string.Empty;
    }

    private void ApplyTable(SortOption sorted)
    {
        NameColumn = ProductTable.NameColumn(sorted, SortDescending);
        var columns = ProductTable.Columns([.. SortOptions], sorted, SortDescending, IsNarrow, SelectedExtraColumn);

        // Headers are replaced only when they change, so tapping or typing does not rebuild them for nothing
        if (!Columns.SequenceEqual(columns))
        {
            Columns.Clear();
            foreach (var column in columns)
            {
                Columns.Add(column);
            }
        }

        Rows.Clear();
        foreach (var product in Products)
        {
            var match = _matches[product.Id];
            Rows.Add(ProductTable.Row(product, columns) with { NameRanges = match.NameRanges, IsSimilar = match.IsFuzzy });
        }
    }
}
