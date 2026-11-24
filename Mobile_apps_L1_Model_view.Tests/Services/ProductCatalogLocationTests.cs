using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ProductCatalogLocationTests
{
    private readonly TestStore _store = new();

    [Fact]
    public async Task GetSummaryAsync_CarriesThePhotoPath()
    {
        var product = await _store.AddProductAsync("Drill", "4006381333931");
        product.PhotoPath = "photos/a.jpg";
        await _store.Products.UpdateAsync(product);

        var summary = await _store.Catalog.GetSummaryAsync(product.Id);

        Assert.Equal("photos/a.jpg", summary!.PhotoPath);
    }

    [Fact]
    public async Task GetDetailsAsync_HistoryItemsCarryTheLocation()
    {
        await _store.SignInAsync(Role.Driver);
        var product = await _store.AddProductAsync("Drill", "4006381333931");
        await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 2);
        _store.Clock.UtcNow = _store.Clock.UtcNow.AddMinutes(1);
        await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 3, location: new GeoPoint(50.45, 30.52));

        var history = (await _store.Catalog.GetDetailsAsync(product.Id))!.History;

        Assert.True(history[0].HasLocation);
        Assert.Equal(new GeoPoint(50.45, 30.52), history[0].Location);
        Assert.Equal("Location 50.45000, 30.52000", history[0].LocationText);
        Assert.False(history[1].HasLocation);
        Assert.Equal(string.Empty, history[1].LocationText);
    }

    [Fact]
    public async Task GetLocatedOperationsAsync_ReturnsOnlyOperationsWithALocation_NewestFirst()
    {
        var driver = await _store.SignInAsync(Role.Driver, "Ivan Driver");
        var drill = await _store.AddProductAsync("Drill", "4006381333931");
        var saw = await _store.AddProductAsync("Saw", "4820001000016");
        await _store.Stock.RecordAsync(drill.Id, OperationDirection.In, 1);
        _store.Clock.UtcNow = _store.Clock.UtcNow.AddMinutes(1);
        await _store.Stock.RecordAsync(drill.Id, OperationDirection.In, 2, location: new GeoPoint(1, 2));
        _store.Clock.UtcNow = _store.Clock.UtcNow.AddMinutes(1);
        await _store.Stock.RecordAsync(saw.Id, OperationDirection.In, 5, location: new GeoPoint(3, 4));

        var located = await _store.Catalog.GetLocatedOperationsAsync();

        Assert.Equal(2, located.Count);
        Assert.Equal("Saw", located[0].ProductName);
        Assert.Equal(new GeoPoint(3, 4), located[0].Location);
        Assert.Equal("In 5 x Saw", located[0].Label);
        Assert.Equal("Drill", located[1].ProductName);
        Assert.Equal(driver.FullName, located[1].UserName);
        Assert.Contains("Ivan Driver", located[1].Details);
    }

    [Fact]
    public async Task GetLocatedOperationsAsync_WithoutAnyLocation_IsEmpty()
    {
        await _store.SignInAsync(Role.Driver);
        var drill = await _store.AddProductAsync("Drill", "4006381333931");
        await _store.Stock.RecordAsync(drill.Id, OperationDirection.In, 1);

        Assert.Empty(await _store.Catalog.GetLocatedOperationsAsync());
    }
}
