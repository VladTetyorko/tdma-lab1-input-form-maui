using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

public partial class ProductListViewModel(
    IProductRepository products,
    IStockService stock,
    ICurrentUserService currentUser,
    INavigationService navigation) : ObservableObject
{
    public ObservableCollection<ProductListItem> Products { get; } = [];

    [ObservableProperty]
    public partial string CurrentUserName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
    public partial bool CanAddProduct { get; set; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var user = currentUser.CurrentUser;
        CurrentUserName = user is null ? string.Empty : $"{user.FullName} ({user.Role})";
        CanAddProduct = user is not null && RolePermissions.CanManageProducts(user.Role);

        var quantities = await stock.GetQuantitiesAsync();
        Products.Clear();
        foreach (var product in await products.GetAllAsync())
        {
            quantities.TryGetValue(product.Id, out var quantity);
            Products.Add(new ProductListItem(product.Id, product.Name, product.Barcode, quantity));
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddProduct))]
    private Task AddProductAsync() => navigation.GoToAsync(Routes.ProductForm);

    [RelayCommand]
    private Task SignOutAsync()
    {
        currentUser.SignOut();
        return navigation.GoToAsync(Routes.Absolute(Routes.Login));
    }
}
