using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using SQLite;

namespace Mobile_apps_L1_Model_view.Tests.Data;

// Migrations/0004_device.sql (photo path, operation location) on a migrated temporary database:
// the CHECK constraints with raw SQL, and the mapping of the repositories plus the sync payload
public class DeviceMigrationTests : IDisposable
{
    private const string Now = "2026-01-15T09:30:00.0000000Z";

    private readonly ServiceProvider _provider = new ServiceCollection().AddStockRoomCore().AddFakePlatform()
        .BuildServiceProvider();

    public void Dispose() => _provider.Dispose();

    private async Task<SQLiteAsyncConnection> Db() =>
        await _provider.GetRequiredService<IDatabase>().GetConnectionAsync();

    private static string NewId() => RowFormat.Text(Guid.CreateVersion7());

    private async Task<string> ExistingProductIdAsync() =>
        await (await Db()).ExecuteScalarAsync<string>("SELECT Id FROM Products ORDER BY rowid LIMIT 1");

    private async Task InsertOperationAsync(double? latitude, double? longitude)
    {
        await (await Db()).ExecuteAsync(
            "INSERT INTO Operations (Id, ProductId, Direction, Quantity, Latitude, Longitude, CreatedByUserId, CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
            NewId(), await ExistingProductIdAsync(), "In", 1, latitude, longitude, RowFormat.Text(SeedData.ManagerId), Now, Now);
    }

    [Fact]
    public async Task SeededRows_HaveNoPhotoAndNoLocation()
    {
        var connection = await Db();

        Assert.Equal(0, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products WHERE PhotoPath IS NOT NULL"));
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Operations WHERE Latitude IS NOT NULL OR Longitude IS NOT NULL"));
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(90.0, 180.0)]
    [InlineData(-90.0, -180.0)]
    [InlineData(50.4501, 30.5234)]
    public async Task Operations_ValidCoordinates_AreAccepted(double latitude, double longitude)
    {
        await InsertOperationAsync(latitude, longitude);
    }

    [Fact]
    public async Task Operations_NoCoordinates_AreAccepted()
    {
        await InsertOperationAsync(null, null);
    }

    [Theory]
    [InlineData(90.1, 0.0)]
    [InlineData(-91.0, 0.0)]
    [InlineData(0.0, 180.1)]
    [InlineData(0.0, -181.0)]
    public async Task Operations_CoordinatesOutOfRange_AreRejected(double latitude, double longitude)
    {
        await Assert.ThrowsAsync<SQLiteException>(() => InsertOperationAsync(latitude, longitude));
    }

    [Fact]
    public async Task Operations_OnlyLatitude_IsRejected()
    {
        await Assert.ThrowsAsync<SQLiteException>(() => InsertOperationAsync(50.0, null));
    }

    [Fact]
    public async Task Operations_OnlyLongitude_IsRejected()
    {
        await Assert.ThrowsAsync<SQLiteException>(() => InsertOperationAsync(null, 30.0));
    }

    [Theory]
    [InlineData("photos/4006381333931_20260115_093000.jpg", true)]
    [InlineData("", false)]
    [InlineData("/etc/passwd", false)]
    [InlineData("photos/../stockroom.db3", false)]
    public async Task Products_PhotoPath_MustBeNullOrARelativePath(string path, bool accepted)
    {
        var connection = await Db();
        var insert = connection.ExecuteAsync(
            "INSERT INTO Products (Id, Name, Barcode, PhotoPath, CreatedByUserId, CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, ?, ?, ?)",
            NewId(), "Photographed", "4820001000016", path, RowFormat.Text(SeedData.ManagerId), Now, Now);

        if (accepted)
        {
            await insert;
        }
        else
        {
            await Assert.ThrowsAsync<SQLiteException>(() => insert);
        }
    }

    [Fact]
    public async Task Product_PhotoPath_RoundTripsAndTravelsInTheSyncPayload()
    {
        var products = _provider.GetRequiredService<IProductRepository>();
        var queue = _provider.GetRequiredService<ISyncQueue>();
        var product = new Product
        {
            Name = "Photographed", Barcode = "4820001000016", PhotoPath = "photos/4820001000016_20260115_093000.jpg",
            CreatedByUserId = SeedData.ManagerId
        };

        await products.AddAsync(product);

        var back = await products.GetByIdAsync(product.Id);
        Assert.Equal("photos/4820001000016_20260115_093000.jpg", back!.PhotoPath);
        var item = (await queue.GetPendingAsync()).Single(i => i.EntityId == product.Id);
        Assert.Contains("\"PhotoPath\":\"photos/4820001000016_20260115_093000.jpg\"", item.PayloadJson);
    }

    [Fact]
    public async Task Product_WithoutPhoto_ReadsBackNull()
    {
        var products = _provider.GetRequiredService<IProductRepository>();
        var product = new Product { Name = "Plain", Barcode = "4820001000016", CreatedByUserId = SeedData.ManagerId };
        await products.AddAsync(product);

        Assert.Null((await products.GetByIdAsync(product.Id))!.PhotoPath);
    }

    [Fact]
    public async Task Operation_Location_RoundTripsAndTravelsInTheSyncPayload()
    {
        var operations = _provider.GetRequiredService<IOperationRepository>();
        var queue = _provider.GetRequiredService<ISyncQueue>();
        var productId = Guid.Parse(await ExistingProductIdAsync());
        var operation = new Operation
        {
            ProductId = productId, Direction = OperationDirection.In, Quantity = 2,
            Location = new GeoPoint(50.4501, 30.5234), CreatedByUserId = SeedData.DriverId
        };

        await operations.AddAsync(operation);

        var back = (await operations.GetByProductAsync(productId)).Single(o => o.Id == operation.Id);
        Assert.Equal(new GeoPoint(50.4501, 30.5234), back.Location);
        var item = (await queue.GetPendingAsync()).Single(i => i.EntityId == operation.Id);
        Assert.Contains("\"Latitude\":50.4501", item.PayloadJson);
        Assert.Contains("\"Longitude\":30.5234", item.PayloadJson);
    }

    [Fact]
    public async Task Operation_WithoutLocation_ReadsBackNull_AndPayloadHasNullCoordinates()
    {
        var operations = _provider.GetRequiredService<IOperationRepository>();
        var queue = _provider.GetRequiredService<ISyncQueue>();
        var productId = Guid.Parse(await ExistingProductIdAsync());
        var operation = new Operation
        {
            ProductId = productId, Direction = OperationDirection.In, Quantity = 2, CreatedByUserId = SeedData.DriverId
        };

        await operations.AddAsync(operation);

        Assert.Null((await operations.GetByProductAsync(productId)).Single(o => o.Id == operation.Id).Location);
        var item = (await queue.GetPendingAsync()).Single(i => i.EntityId == operation.Id);
        Assert.Contains("\"Latitude\":null", item.PayloadJson);
    }
}
