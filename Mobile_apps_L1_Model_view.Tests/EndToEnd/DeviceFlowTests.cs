using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.EndToEnd;

// Lab 7 flows through the real container (real services, real SQLite, fake platform): sign in through the login page,
// then use the device capabilities through the ViewModels
public class DeviceFlowTests : IDisposable
{
    private static readonly GeoPoint DropOff = new(50.4501, 30.5234);

    private readonly ServiceProvider _provider;
    private readonly FakeNavigationService _navigation = new();

    public DeviceFlowTests()
    {
        var services = new ServiceCollection().AddStockRoomCore().AddFakePlatform();
        services.AddSingleton<INavigationService>(_navigation);
        services.AddSingleton<IPreferencesStore>(new FakePreferencesStore());
        services.AddSingleton<IAlertService>(new FakeAlertService());
        _provider = services.BuildServiceProvider();
    }

    public void Dispose() => _provider.Dispose();

    private T Get<T>() where T : notnull => _provider.GetRequiredService<T>();

    private async Task<ProductSummary> NutellaAsync()
    {
        var list = Get<ProductListViewModel>();
        await list.LoadCommand.ExecuteAsync(null);
        return Assert.Single(list.Products, p => p.Name == "Nutella 400 g");
    }

    private OperationViewModel OpenOperation(ProductSummary product)
    {
        var operation = Get<OperationViewModel>();
        operation.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = product });
        return operation;
    }

    [Fact]
    public async Task Driver_AttachesLocationToAnIn_ItIsSavedShownInTheHistoryAndOnTheMap()
    {
        await DemoAccounts.SignInAsync(_provider, Role.Driver);
        Get<FakeLocationService>().Result = LocationResult.Found(DropOff);
        var product = await NutellaAsync();

        var operation = OpenOperation(product);
        await operation.LoadCommand.ExecuteAsync(null);
        operation.Quantity = 5;
        await operation.AttachLocationCommand.ExecuteAsync(null);
        Assert.Equal("Location attached: 50.45010, 30.52340", operation.LocationText);
        await operation.SaveCommand.ExecuteAsync(null);

        // persisted
        var stored = (await Get<IOperationRepository>().GetByProductAsync(product.Id)).Single(o => o.Location is not null);
        Assert.Equal(DropOff, stored.Location);
        Assert.Equal(5, stored.Quantity);

        // visible in the history of the product
        var detail = Get<ProductDetailViewModel>();
        detail.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = product });
        await detail.LoadCommand.ExecuteAsync(null);
        var line = Assert.Single(detail.History, h => h.HasLocation);
        Assert.Equal(DropOff, line.Location);

        // "Open in maps" from the history goes to the maps launcher
        await detail.OpenLocationCommand.ExecuteAsync(line);
        Assert.Equal(DropOff, Assert.Single(Get<FakeMapLauncher>().Opened).Location);

        // and on the map page
        var map = Get<MapViewModel>();
        await map.LoadCommand.ExecuteAsync(null);
        var pin = Assert.Single(map.Pins);
        Assert.Equal(DropOff, pin.Location);
        Assert.Equal("Nutella 400 g", pin.ProductName);
        Assert.Equal("In 5 x Nutella 400 g", pin.Label);
    }

    [Fact]
    public async Task LocationDenied_StillLetsTheOperationBeSavedWithoutOne()
    {
        await DemoAccounts.SignInAsync(_provider, Role.Driver);
        Get<FakeLocationService>().Result = LocationResult.Of(DeviceStatus.PermissionDenied);
        var product = await NutellaAsync();

        var operation = OpenOperation(product);
        await operation.LoadCommand.ExecuteAsync(null);
        await operation.AttachLocationCommand.ExecuteAsync(null);
        await operation.SaveCommand.ExecuteAsync(null);

        Assert.Contains("Location permission was denied", operation.LocationMessage);
        Assert.DoesNotContain(await Get<IOperationRepository>().GetAllAsync(), o => o.Location is not null);
        var map = Get<MapViewModel>();
        await map.LoadCommand.ExecuteAsync(null);
        Assert.Empty(map.Pins);
    }

    [Fact]
    public async Task OperationLocation_TravelsInTheSyncQueue_ButNoPhotoBytesDo()
    {
        await DemoAccounts.SignInAsync(_provider, Role.Driver);
        var product = await NutellaAsync();
        var operation = OpenOperation(product);
        await operation.LoadCommand.ExecuteAsync(null);
        await operation.AttachLocationCommand.ExecuteAsync(null);
        await operation.SaveCommand.ExecuteAsync(null);

        var queued = (await Get<ISyncQueue>().GetPendingAsync()).Where(i => i.EntityType == "Operations").ToList();

        var payload = Assert.Single(queued, i => i.PayloadJson.Contains("\"Latitude\":50.4501")).PayloadJson;
        Assert.Contains("\"Longitude\":30.5234", payload);
    }

    [Fact]
    public async Task Manager_TakesAPhotoForANewProduct_AndTheDetailPageShowsIt()
    {
        await DemoAccounts.SignInAsync(_provider, Role.Manager);
        var form = Get<ProductFormViewModel>();
        form.Name = "Cordless Saw";
        form.Barcode = "4006381333931";

        await form.TakePhotoCommand.ExecuteAsync(null);
        var photo = Get<FakePhotoService>();
        var path = Assert.Single(photo.CaptureBarcodes);
        Assert.Equal("4006381333931", path);
        await form.SaveCommand.ExecuteAsync(null);

        var saved = await Get<IProductRepository>().GetByBarcodeAsync("4006381333931");
        Assert.Equal(form.Photo.Path, saved!.PhotoPath);
        Assert.False(Path.IsPathRooted(saved.PhotoPath));

        var detail = Get<ProductDetailViewModel>();
        detail.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = (await Get<IProductCatalog>().GetSummaryByBarcodeAsync("4006381333931"))!
        });
        await detail.LoadCommand.ExecuteAsync(null);
        Assert.Equal(saved.PhotoPath, detail.Photo.Path);
        Assert.NotNull(detail.Photo.ResolvedPath);
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task NonManager_CannotTakeAPhoto(Role role)
    {
        await DemoAccounts.SignInAsync(_provider, role);
        var product = await NutellaAsync();
        var detail = Get<ProductDetailViewModel>();
        detail.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = product });
        await detail.LoadCommand.ExecuteAsync(null);

        await detail.TakePhotoCommand.ExecuteAsync(null);

        Assert.Equal($"Your role ({role}) cannot change product photos.", detail.StatusMessage);
        Assert.Empty(Get<FakePhotoService>().CaptureBarcodes);
    }
}
