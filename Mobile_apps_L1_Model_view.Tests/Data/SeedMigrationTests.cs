using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Data.Migrations;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Data;

// The demo data comes from Migrations/0002_seed.sql, applied when the database is first opened
public class SeedMigrationTests : IDisposable
{
    private readonly ServiceProvider _provider = new ServiceCollection().AddStockRoomCore().AddFakePlatform()
        .BuildServiceProvider();

    private IUserRepository _users => _provider.GetRequiredService<IUserRepository>();
    private IProductRepository _products => _provider.GetRequiredService<IProductRepository>();
    private ICharacteristicRepository _characteristics => _provider.GetRequiredService<ICharacteristicRepository>();
    private IProductCharacteristicRepository _values => _provider.GetRequiredService<IProductCharacteristicRepository>();
    private IOperationRepository _operations => _provider.GetRequiredService<IOperationRepository>();

    public void Dispose() => _provider.Dispose();

    [Fact]
    public async Task Seed_CreatesOneUserPerRole()
    {
        var users = await _users.GetAllAsync();
        Assert.Equal(3, users.Count);
        Assert.Equal(
            [Role.Storekeeper, Role.Driver, Role.Manager],
            users.Select(u => u.Role).OrderBy(r => r).ToArray());
    }

    [Fact]
    public async Task Seed_CreatesFourCharacteristics()
    {
        Assert.Equal(4, (await _characteristics.GetAllAsync()).Count);
    }

    [Fact]
    public async Task Seed_CreatesTenProductsWithValidAndUniqueBarcodes()
    {
        var products = await _products.GetAllAsync();
        Assert.Equal(10, products.Count);
        Assert.All(products, p => Assert.True(Ean13.IsValid(p.Barcode), $"Invalid barcode for {p.Name}"));
        Assert.Equal(10, products.Select(p => p.Barcode).Distinct().Count());
    }

    [Fact]
    public async Task Seed_GivesEveryProductCharacteristicValues()
    {
        foreach (var product in await _products.GetAllAsync())
        {
            Assert.Equal(4, (await _values.GetByProductAsync(product.Id)).Count);
        }
    }

    [Fact]
    public async Task Seed_OperationsHavePositiveQuantityAndExistingProduct()
    {
        var operations = await _operations.GetAllAsync();
        Assert.NotEmpty(operations);
        foreach (var operation in operations)
        {
            Assert.True(operation.Quantity > 0, $"Quantity must be positive for operation {operation.Id}");
            Assert.NotNull(await _products.GetByIdAsync(operation.ProductId));
        }
    }

    [Fact]
    public async Task Seed_SeedsStockForProductsWithMovements()
    {
        var drill = await _products.GetByBarcodeAsync(Ean13.WithCheckDigit("482000100001"));
        Assert.NotNull(drill);
        var drillOperations = await _operations.GetByProductAsync(drill.Id);
        var onHand = drillOperations.Sum(o => o.Direction == OperationDirection.In ? o.Quantity : -o.Quantity);
        Assert.Equal(21, onHand);
    }

    [Fact]
    public async Task Seed_UsesTheFixedUserIds()
    {
        Assert.Equal(Role.Manager, (await _users.GetByIdAsync(SeedData.ManagerId))!.Role);
        Assert.Equal(Role.Storekeeper, (await _users.GetByIdAsync(SeedData.StorekeeperId))!.Role);
        Assert.Equal(Role.Driver, (await _users.GetByIdAsync(SeedData.DriverId))!.Role);
    }

    [Fact]
    public async Task Seed_OpeningTheDatabaseAgain_AddsNothing()
    {
        var before = (await _products.GetAllAsync()).Count;

        var paths = _provider.GetRequiredService<IDatabasePathProvider>();
        var reopened = new SqliteDatabase(paths, _provider.GetRequiredService<MigrationRunner>());
        var products = new SqliteProductRepository(reopened, _provider.GetRequiredService<SyncJournal>());

        Assert.Equal(before, (await products.GetAllAsync()).Count);
        reopened.Dispose();
    }
}
