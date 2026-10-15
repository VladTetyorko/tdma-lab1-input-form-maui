using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using SQLite;

namespace Mobile_apps_L1_Model_view.Tests.Data;

// The real schema in a real temporary SQLite file
public class SqliteDatabaseTests : IDisposable
{
    private readonly ServiceProvider _provider = new ServiceCollection().AddStockRoomCore().AddFakePlatform()
        .BuildServiceProvider();

    public void Dispose() => _provider.Dispose();

    private async Task<SQLiteAsyncConnection> OpenAsync() =>
        await _provider.GetRequiredService<IDatabase>().GetConnectionAsync();

    [Fact]
    public async Task Open_AppliesAllMigrations_AndSetsTheVersion()
    {
        var connection = await OpenAsync();

        Assert.Equal(2, await connection.ExecuteScalarAsync<int>("PRAGMA user_version"));
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>("PRAGMA foreign_keys"));
    }

    [Fact]
    public async Task Insert_ProductWithDuplicateBarcode_IsRejectedByTheDatabase()
    {
        var products = _provider.GetRequiredService<IProductRepository>();
        var existing = (await products.GetAllAsync())[0];

        var duplicate = new Product { Name = "Copy", Barcode = existing.Barcode, CreatedByUserId = existing.CreatedByUserId };

        await Assert.ThrowsAsync<SQLiteException>(() => products.AddAsync(duplicate));
    }

    [Fact]
    public async Task Insert_ProductOfUnknownUser_IsRejectedByTheForeignKey()
    {
        var products = _provider.GetRequiredService<IProductRepository>();

        var product = new Product { Name = "Orphan", Barcode = "4006381333931", CreatedByUserId = Guid.NewGuid() };

        await Assert.ThrowsAsync<SQLiteException>(() => products.AddAsync(product));
    }

    [Fact]
    public async Task Insert_OperationWithZeroQuantity_IsRejectedByTheCheckConstraint()
    {
        var products = _provider.GetRequiredService<IProductRepository>();
        var operations = _provider.GetRequiredService<IOperationRepository>();
        var product = (await products.GetAllAsync())[0];

        var operation = new Operation
        {
            ProductId = product.Id, Direction = OperationDirection.In, Quantity = 0, CreatedByUserId = SeedData.DriverId
        };

        await Assert.ThrowsAsync<SQLiteException>(() => operations.AddAsync(operation));
    }

    [Fact]
    public async Task Insert_CharacteristicWithSameNameInOtherCase_IsRejected()
    {
        var characteristics = _provider.GetRequiredService<ICharacteristicRepository>();

        var duplicate = new Characteristic { Name = "COLOR", ValueType = CharacteristicValueType.Text, CreatedByUserId = SeedData.ManagerId };

        await Assert.ThrowsAsync<SQLiteException>(() => characteristics.AddAsync(duplicate));
    }

    [Fact]
    public async Task Write_IsQueuedInTheSameTransaction()
    {
        var queue = _provider.GetRequiredService<ISyncQueue>();
        var products = _provider.GetRequiredService<IProductRepository>();
        Assert.Equal(0, await queue.CountAsync());

        var product = new Product { Name = "Saw", Barcode = "4006381333931", CreatedByUserId = SeedData.ManagerId };
        await products.AddAsync(product);
        product.Name = "Saw 2";
        product.UpdatedAt = DateTime.UtcNow;
        await products.UpdateAsync(product);
        await products.DeleteAsync(product.Id);

        var items = await queue.GetPendingAsync();
        Assert.Equal([SyncOperation.Insert, SyncOperation.Update, SyncOperation.Delete], items.Select(i => i.Operation));
        Assert.All(items, i => Assert.Equal("Products", i.EntityType));
        Assert.All(items, i => Assert.Equal(product.Id, i.EntityId));
        Assert.Contains("Saw 2", items[2].PayloadJson);
    }

    [Fact]
    public async Task Write_ThatFails_LeavesNothingInTheQueue()
    {
        var queue = _provider.GetRequiredService<ISyncQueue>();
        var products = _provider.GetRequiredService<IProductRepository>();
        var existing = (await products.GetAllAsync())[0];

        await Assert.ThrowsAsync<SQLiteException>(() => products.AddAsync(
            new Product { Name = "Copy", Barcode = existing.Barcode, CreatedByUserId = existing.CreatedByUserId }));

        Assert.Equal(0, await queue.CountAsync());
    }

    [Fact]
    public async Task Counterparty_IsEncryptedInTheFile_AndReadBackAsPlainText()
    {
        var operations = _provider.GetRequiredService<IOperationRepository>();
        var product = (await _provider.GetRequiredService<IProductRepository>().GetAllAsync())[0];
        var operation = new Operation
        {
            ProductId = product.Id, Direction = OperationDirection.In, Quantity = 5,
            Counterparty = "Acme Ltd", CreatedByUserId = SeedData.DriverId
        };

        await operations.AddAsync(operation);

        var stored = await (await OpenAsync()).ExecuteScalarAsync<string>(
            "SELECT Counterparty FROM Operations WHERE Id = ?", operation.Id.ToString());
        Assert.NotNull(stored);
        Assert.DoesNotContain("Acme", stored);
        var read = (await operations.GetByProductAsync(product.Id)).Single(o => o.Id == operation.Id);
        Assert.Equal("Acme Ltd", read.Counterparty);
    }

    [Fact]
    public async Task CopyTo_WritesACompleteReadableDatabase()
    {
        var database = _provider.GetRequiredService<IDatabase>();
        var target = Path.Combine(_provider.GetRequiredService<TempDatabasePath>().ExportDirectory, "copy.db3");

        await database.CopyToAsync(target);

        var copy = new SQLiteAsyncConnection(target, SQLiteOpenFlags.ReadOnly);
        Assert.Equal(10, await copy.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products"));
        await copy.CloseAsync();
    }
}
