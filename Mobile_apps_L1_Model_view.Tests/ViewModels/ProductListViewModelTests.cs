using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class ProductListViewModelTests
{
    private readonly InMemoryProductRepository _products = new();
    private readonly InMemoryOperationRepository _operations = new();
    private readonly CurrentUserService _currentUser = new();
    private readonly FakeNavigationService _navigation = new();
    private readonly ProductListViewModel _viewModel;

    public ProductListViewModelTests()
    {
        var stock = new StockService(_operations, _products, _currentUser, new FakeClock());
        _viewModel = new ProductListViewModel(_products, stock, _currentUser, _navigation);
    }

    [Fact]
    public async Task Load_FillsItemsWithQuantityOnHand()
    {
        var drill = new Product { Name = "Drill", Barcode = "4006381333931" };
        var saw = new Product { Name = "Saw", Barcode = "4820001000016" };
        await _products.AddAsync(drill);
        await _products.AddAsync(saw);
        await _operations.AddAsync(new Operation { ProductId = drill.Id, Direction = OperationDirection.In, Quantity = 10 });
        await _operations.AddAsync(new Operation { ProductId = drill.Id, Direction = OperationDirection.Out, Quantity = 4 });

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, _viewModel.Products.Count);
        Assert.Contains(_viewModel.Products, p => p.Id == drill.Id && p.Name == "Drill" && p.Quantity == 6);
        Assert.Contains(_viewModel.Products, p => p.Id == saw.Id && p.Name == "Saw" && p.Quantity == 0);
    }

    [Fact]
    public async Task Load_ShowsCurrentUserNameAndRole()
    {
        _currentUser.SignIn(new User { FullName = "Olena Kovalenko", Role = Role.Manager });

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Olena Kovalenko (Manager)", _viewModel.CurrentUserName);
    }

    [Theory]
    [InlineData(Role.Manager, true)]
    [InlineData(Role.Storekeeper, false)]
    [InlineData(Role.Driver, false)]
    public async Task CanAddProduct_OnlyForManager(Role role, bool expected)
    {
        _currentUser.SignIn(new User { FullName = "Test", Role = role });

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(expected, _viewModel.CanAddProduct);
        Assert.Equal(expected, _viewModel.AddProductCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddProduct_NavigatesToProductForm()
    {
        _currentUser.SignIn(new User { FullName = "Olena Kovalenko", Role = Role.Manager });
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.AddProductCommand.ExecuteAsync(null);

        Assert.Equal(Routes.ProductForm, Assert.Single(_navigation.RequestedRoutes));
    }

    [Fact]
    public async Task SignOut_SignsOutAndNavigatesToLogin()
    {
        _currentUser.SignIn(new User { FullName = "Olena Kovalenko", Role = Role.Manager });

        await _viewModel.SignOutCommand.ExecuteAsync(null);

        Assert.Null(_currentUser.CurrentUser);
        Assert.Equal(Routes.Absolute(Routes.Login), Assert.Single(_navigation.RequestedRoutes));
    }
}
