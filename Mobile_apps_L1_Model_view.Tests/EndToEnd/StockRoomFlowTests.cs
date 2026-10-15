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
        services.AddSingleton<IPreferencesStore>(new FakePreferencesStore());
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

    [Fact]
    public async Task Manager_EditsWeight_AndListShowsNewValueInSortedPosition()
    {
        await SignInAsync(Role.Manager);
        var list = _provider.GetRequiredService<ProductListViewModel>();
        await list.LoadCommand.ExecuteAsync(null);
        var drill = Assert.Single(list.Products, p => p.Name == "Cordless Drill 18V");

        await list.OpenDetailsCommand.ExecuteAsync(drill);
        var detail = _provider.GetRequiredService<ProductDetailViewModel>();
        detail.ApplyQueryAttributes(LastParameters());
        await detail.LoadCommand.ExecuteAsync(null);
        var weight = detail.Characteristics.Single(c => c.Name == "Weight (kg)");
        Assert.Equal("1.6", weight.Value);

        weight.Value = "9.5";
        Assert.True(detail.SaveCommand.CanExecute(null));
        await detail.SaveCommand.ExecuteAsync(null);

        // Shell hands the parameters of the Back navigation to the list
        Assert.Equal(Routes.Back, _navigation.RequestedRoutes.Last());
        list.ApplyQueryAttributes(LastParameters());

        var updated = Assert.Single(list.Products, p => p.Id == drill.Id);
        Assert.Equal("9.5", updated.Values[weight.CharacteristicId]);

        list.SelectedSortOption = list.SortOptions.Single(o => o.Title == "Weight (kg)");
        Assert.Equal("Interior Paint 10 L", list.Products.Last().Name);
        Assert.Equal(8, list.Products.IndexOf(updated));

        list.SortDescending = true;
        Assert.Equal("Interior Paint 10 L", list.Products[0].Name);
        Assert.Equal(drill.Id, list.Products[1].Id);
    }

    [Fact]
    public async Task Manager_AddsCharacteristic_AndListOffersItAsSortOption()
    {
        await SignInAsync(Role.Manager);
        var list = _provider.GetRequiredService<ProductListViewModel>();
        await list.LoadCommand.ExecuteAsync(null);
        await list.OpenCharacteristicsCommand.ExecuteAsync(null);

        var characteristics = _provider.GetRequiredService<CharacteristicsViewModel>();
        await characteristics.LoadCommand.ExecuteAsync(null);
        characteristics.NewName = "Shelf";
        characteristics.NewValueType = CharacteristicValueType.Number;
        await characteristics.AddCommand.ExecuteAsync(null);
        await characteristics.GoBackCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Back, _navigation.RequestedRoutes.Last());
        list.ApplyQueryAttributes(LastParameters());
        await list.EnsureLoadedCommand.ExecuteAsync(null);

        list.SelectedSortOption = Assert.Single(list.SortOptions, o => o.Title == "Shelf");
        var names = list.Products.Select(p => p.Name).ToList();
        // No product has a Shelf value yet, so every row is "missing" and the order falls back to name
        Assert.Equal(names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase), names);
    }

    [Fact]
    public async Task Storekeeper_RecordsOutFromOperationPage_AndDetailShowsReducedQuantity()
    {
        await SignInAsync(Role.Storekeeper);
        var list = _provider.GetRequiredService<ProductListViewModel>();
        await list.LoadCommand.ExecuteAsync(null);
        var drill = Assert.Single(list.Products, p => p.Name == "Cordless Drill 18V");

        await list.OpenDetailsCommand.ExecuteAsync(drill);
        var detail = _provider.GetRequiredService<ProductDetailViewModel>();
        detail.ApplyQueryAttributes(LastParameters());
        await detail.LoadCommand.ExecuteAsync(null);
        Assert.Equal(21, detail.Quantity);

        await detail.OpenOperationCommand.ExecuteAsync(null);
        var operation = _provider.GetRequiredService<OperationViewModel>();
        operation.ApplyQueryAttributes(LastParameters());
        await operation.LoadCommand.ExecuteAsync(null);
        Assert.Equal(21, operation.QuantityOnHand);

        operation.SelectedDirection = OperationDirection.Out;
        operation.Quantity = 2;
        await operation.SaveCommand.ExecuteAsync(null);
        Assert.Equal(Routes.Back, _navigation.RequestedRoutes.Last());

        // The detail page reloads its data when it appears again
        await detail.LoadCommand.ExecuteAsync(null);
        Assert.Equal(19, detail.Quantity);
        Assert.Equal(OperationDirection.Out, detail.History[0].Direction);
        Assert.Equal(2, detail.History[0].Quantity);
    }

    [Fact]
    public async Task Driver_OperationPageOffersOnlyIn()
    {
        await SignInAsync(Role.Driver);
        var list = _provider.GetRequiredService<ProductListViewModel>();
        await list.LoadCommand.ExecuteAsync(null);
        var drill = Assert.Single(list.Products, p => p.Name == "Cordless Drill 18V");

        await list.OpenDetailsCommand.ExecuteAsync(drill);
        var detail = _provider.GetRequiredService<ProductDetailViewModel>();
        detail.ApplyQueryAttributes(LastParameters());
        await detail.LoadCommand.ExecuteAsync(null);
        Assert.False(detail.CanEdit);

        await detail.OpenOperationCommand.ExecuteAsync(null);
        var operation = _provider.GetRequiredService<OperationViewModel>();
        operation.ApplyQueryAttributes(LastParameters());
        await operation.LoadCommand.ExecuteAsync(null);

        Assert.Equal(new[] { OperationDirection.In }, operation.AvailableDirections);
        Assert.Equal(OperationDirection.In, operation.SelectedDirection);
    }

    // Shell hands the parameters of the last navigation to the page that was opened
    private IDictionary<string, object> LastParameters() => _navigation.RequestedParameters.Last()!;

    private async Task SignInAsync(Role role)
    {
        var login = _provider.GetRequiredService<LoginViewModel>();
        await login.LoadCommand.ExecuteAsync(null);
        login.SelectedUser = login.Users.Single(u => u.Role == role);
        await login.SignInCommand.ExecuteAsync(null);
    }
}
