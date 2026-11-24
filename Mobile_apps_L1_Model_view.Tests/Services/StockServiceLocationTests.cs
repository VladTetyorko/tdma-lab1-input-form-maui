using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class StockServiceLocationTests
{
    private readonly TestStore _store = new();

    [Fact]
    public async Task RecordAsync_WithLocation_StoresItOnTheOperation()
    {
        await _store.SignInAsync(Role.Driver);
        var product = await _store.AddProductAsync("Drill", "4006381333931");
        var place = new GeoPoint(50.4501, 30.5234);

        var result = await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 3, location: place);

        Assert.True(result.IsSuccess);
        Assert.Equal(place, result.Value!.Location);
        Assert.Equal(place, Assert.Single(await _store.Operations.GetAllAsync()).Location);
    }

    [Fact]
    public async Task RecordAsync_WithoutLocation_LeavesItEmpty()
    {
        await _store.SignInAsync(Role.Driver);
        var product = await _store.AddProductAsync("Drill", "4006381333931");

        var result = await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 3);

        Assert.Null(result.Value!.Location);
    }

    [Fact]
    public async Task RecordAsync_LogsOnlyWhetherThereWasALocation_NotTheCoordinates()
    {
        await _store.SignInAsync(Role.Driver);
        var product = await _store.AddProductAsync("Drill", "4006381333931");

        await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 3, location: new GeoPoint(50.4501, 30.5234));

        var line = _store.Logs.Single(l => l.Message.StartsWith("Recorded"));
        Assert.Contains("location: True", line.Message);
        Assert.DoesNotContain("50.45", line.Message);
    }
}
