using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

// Search combined with the sort of the product list
public class ProductListSearchTests
{
    private readonly TestStore _store = new();
    private readonly ProductListViewModel _viewModel;

    public ProductListSearchTests()
    {
        _viewModel = new ProductListViewModel(_store.Catalog, _store.CharacteristicService, _store.ProductService, _store.CurrentUser, _store.Startup, _store.Alerts, _store.Guard, _store.Navigation,
            _store.ShakeGesture, _store.Logger<ProductListViewModel>());
    }

    [Fact]
    public async Task SearchText_ShowsOnlyMatchingProducts_AndClearingItShowsAllAgain()
    {
        await AddSampleProductsAsync();

        _viewModel.SearchText = "nutella";
        Assert.Equal(new[] { "Nutella 400 g", "Nutella 500 g" }, _viewModel.Products.Select(p => p.Name));

        _viewModel.SearchText = string.Empty;
        Assert.Equal(3, _viewModel.Products.Count);
    }

    [Fact]
    public async Task SearchText_MatchesTheBarcode()
    {
        await AddSampleProductsAsync();

        _viewModel.SearchText = "4006381";

        Assert.Equal("Cordless Drill", Assert.Single(_viewModel.Products).Name);
    }

    [Fact]
    public async Task SearchText_IsCombinedWithTheSort()
    {
        await AddSampleProductsAsync();

        _viewModel.SearchText = "nutella";
        _viewModel.SelectedSortOption = SortOption.ByQuantity;
        Assert.Equal(new[] { "Nutella 500 g", "Nutella 400 g" }, _viewModel.Products.Select(p => p.Name));

        _viewModel.SortDescending = true;
        Assert.Equal(new[] { "Nutella 400 g", "Nutella 500 g" }, _viewModel.Products.Select(p => p.Name));
    }

    [Fact]
    public async Task ChangingTheSort_KeepsTheSearch()
    {
        await AddSampleProductsAsync();
        _viewModel.SearchText = "drill";

        _viewModel.SelectedSortOption = SortOption.ByQuantity;
        _viewModel.SortDescending = true;

        Assert.Equal("Cordless Drill", Assert.Single(_viewModel.Products).Name);
    }

    [Fact]
    public async Task NoMatch_ShowsAnEmptyListAndAMessage_ThatDisappearsWhenTheTextMatches()
    {
        await AddSampleProductsAsync();

        _viewModel.SearchText = "zzz";
        Assert.Empty(_viewModel.Products);
        Assert.Equal("No products match \"zzz\".", _viewModel.EmptyText);

        _viewModel.SearchText = "drill";
        Assert.Single(_viewModel.Products);
        Assert.Equal(string.Empty, _viewModel.EmptyText);
    }

    [Theory]
    [InlineData(Role.Manager, "Add the first product.")]
    [InlineData(Role.Storekeeper, "A manager adds them.")]
    [InlineData(Role.Driver, "A manager adds them.")]
    public async Task EmptyWarehouse_SaysWhoCanFillIt(Role role, string hint)
    {
        await _store.SignInAsync(role);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(_viewModel.IsEmptyShown);
        Assert.Equal(("No products yet.", hint, string.Empty),
            (_viewModel.EmptyText, _viewModel.EmptyHint, _viewModel.EmptyActionText));
    }

    [Fact]
    public async Task ClearSearch_ShowsEverythingAgain()
    {
        await AddSampleProductsAsync();
        _viewModel.SearchText = "zzz";
        Assert.Equal("Clear search", _viewModel.EmptyActionText);

        _viewModel.ClearSearchCommand.Execute(null);

        Assert.Equal(3, _viewModel.Products.Count);
        Assert.False(_viewModel.IsEmptyShown);
    }

    [Fact]
    public async Task Refresh_ReadsTheProductsAgainAndStopsTheSpinner()
    {
        await AddSampleProductsAsync();
        await _store.AddProductAsync("Hammer", Ean13.WithCheckDigit("482000100009"));
        _viewModel.IsRefreshing = true;

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(4, _viewModel.Products.Count);
        Assert.False(_viewModel.IsRefreshing);
    }

    [Fact]
    public async Task AProductReturnedFromAnotherPage_IsListedOnlyWhenItMatchesTheSearch()
    {
        await AddSampleProductsAsync();
        _viewModel.SearchText = "drill";

        var added = new ProductSummary(Guid.NewGuid(), "Nutella Biscuits", "8000500310427", 2);
        _viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationKeys.UpdatedProduct] = added });
        Assert.Equal("Cordless Drill", Assert.Single(_viewModel.Products).Name);

        _viewModel.SearchText = "biscuits";
        Assert.Equal("Nutella Biscuits", Assert.Single(_viewModel.Products).Name);
    }

    [Fact]
    public async Task TheSearchIsNotSavedAsADraft()
    {
        await AddSampleProductsAsync();

        _viewModel.SearchText = "nutella";

        Assert.Empty(_store.Preferences.Values);
        Assert.Null(_store.AppState.GetDraft<ProductFormDraft>());
    }

    private async Task AddSampleProductsAsync()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var big = await _store.AddProductAsync("Nutella 400 g", "3017620422003");
        var small = await _store.AddProductAsync("Nutella 500 g", "4008400402222");
        await _store.AddProductAsync("Cordless Drill", "4006381333931");
        await _store.Operations.AddAsync(new Operation { ProductId = big.Id, Direction = OperationDirection.In, Quantity = 9 });
        await _store.Operations.AddAsync(new Operation { ProductId = small.Id, Direction = OperationDirection.In, Quantity = 2 });
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
