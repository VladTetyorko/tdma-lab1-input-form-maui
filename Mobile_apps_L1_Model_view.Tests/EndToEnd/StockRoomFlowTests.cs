using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.EndToEnd;

// Whole user flows through the real DI container: real services and repositories, fake navigation
public class StockRoomFlowTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly FakeNavigationService _navigation = new();

    public StockRoomFlowTests()
    {
        var services = new ServiceCollection().AddStockRoomCore();
        services.AddSingleton<INavigationService>(_navigation);
        _provider = services.BuildServiceProvider();
        _provider.GetRequiredService<IDataSeeder>().SeedAsync().GetAwaiter().GetResult();
    }

    public void Dispose() => _provider.Dispose();

    [Fact]
    public async Task Manager_AddsProduct_AndListShowsItWithZeroQuantity()
    {
        await SignInAsync(Role.Manager);

        var list = _provider.GetRequiredService<ProductListViewModel>();
        await list.LoadCommand.ExecuteAsync(null);
        Assert.True(list.CanAddProduct);
        await list.AddProductCommand.ExecuteAsync(null);
        Assert.Equal(Routes.ProductForm, _navigation.RequestedRoutes.Last());

        var form = _provider.GetRequiredService<ProductFormViewModel>();
        form.Name = "Cordless Saw";
        form.Barcode = "4006381333931";
        Assert.True(form.SaveCommand.CanExecute(null));
        await form.SaveCommand.ExecuteAsync(null);
        Assert.Equal(Routes.Back, _navigation.RequestedRoutes.Last());

        await list.LoadCommand.ExecuteAsync(null);
        var item = Assert.Single(list.Products, p => p.Barcode == "4006381333931");
        Assert.Equal("Cordless Saw", item.Name);
        Assert.Equal(0, item.Quantity);
    }

    [Fact]
    public async Task Storekeeper_RecordsOutOfThreeUnits_AndListShowsReducedQuantity()
    {
        await SignInAsync(Role.Storekeeper);

        var list = _provider.GetRequiredService<ProductListViewModel>();
        await list.LoadCommand.ExecuteAsync(null);
        var drill = Assert.Single(list.Products, p => p.Name == "Cordless Drill 18V");
        Assert.Equal(21, drill.Quantity);

        var stock = _provider.GetRequiredService<IStockService>();
        var result = await stock.RecordAsync(drill.Id, OperationDirection.Out, 3);
        Assert.True(result.IsSuccess, result.Error);

        await list.LoadCommand.ExecuteAsync(null);
        var updated = Assert.Single(list.Products, p => p.Id == drill.Id);
        Assert.Equal(18, updated.Quantity);
    }

    [Fact]
    public async Task Driver_OutIsRejected_ButInOfFiveIsAccepted_AndQuantityGrowsByFive()
    {
        await SignInAsync(Role.Driver);

        var list = _provider.GetRequiredService<ProductListViewModel>();
        await list.LoadCommand.ExecuteAsync(null);
        var hammer = Assert.Single(list.Products, p => p.Name == "Claw Hammer 500 g");
        Assert.Equal(28, hammer.Quantity);

        var stock = _provider.GetRequiredService<IStockService>();

        var rejected = await stock.RecordAsync(hammer.Id, OperationDirection.Out, 3);
        Assert.False(rejected.IsSuccess);
        Assert.Equal("Your role (Driver) cannot record Out operations.", rejected.Error);

        var accepted = await stock.RecordAsync(hammer.Id, OperationDirection.In, 5);
        Assert.True(accepted.IsSuccess, accepted.Error);

        await list.LoadCommand.ExecuteAsync(null);
        var updated = Assert.Single(list.Products, p => p.Id == hammer.Id);
        Assert.Equal(33, updated.Quantity);
    }

    private async Task SignInAsync(Role role)
    {
        var login = _provider.GetRequiredService<LoginViewModel>();
        await login.LoadCommand.ExecuteAsync(null);
        login.SelectedUser = login.Users.Single(u => u.Role == role);
        await login.SignInCommand.ExecuteAsync(null);
    }
}
