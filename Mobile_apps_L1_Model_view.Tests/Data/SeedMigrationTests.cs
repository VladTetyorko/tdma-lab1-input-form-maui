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

    private const string NutellaBarcode = "3017620422003";

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
    public async Task Seed_CreatesTwelveRealProductsWithValidAndUniqueBarcodes()
    {
        var products = await _products.GetAllAsync();
        Assert.Equal(12, products.Count);
        Assert.All(products, p => Assert.True(Ean13.IsValid(p.Barcode), $"Invalid barcode for {p.Name}"));
        Assert.Equal(12, products.Select(p => p.Barcode).Distinct().Count());
        Assert.DoesNotContain(products, p => p.Barcode.StartsWith("4820001"));
    }

    [Fact]
    public async Task Seed_EveryProductHasBrandCategoryAndOneSize()
    {
        var characteristics = (await _characteristics.GetAllAsync()).ToDictionary(c => c.Id, c => c.Name);
        foreach (var product in await _products.GetAllAsync())
        {
            var values = await _values.GetByProductAsync(product.Id);
            Assert.All(values, v => Assert.True(characteristics.ContainsKey(v.CharacteristicId)));
            var names = values.Select(v => characteristics[v.CharacteristicId]).ToList();
            Assert.Contains("Brand", names);
            Assert.Contains("Category", names);
            Assert.Single(names, n => n is "Weight (kg)" or "Volume (l)");
            Assert.Equal(names.Count, names.Distinct().Count());
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
        var nutella = await _products.GetByBarcodeAsync(NutellaBarcode);
        Assert.NotNull(nutella);
        var operations = await _operations.GetByProductAsync(nutella.Id);
        var onHand = operations.Sum(o => o.Direction == OperationDirection.In ? o.Quantity : -o.Quantity);
        Assert.Equal(36, onHand);
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
        var products = new SqliteProductRepository(reopened, _provider.GetRequiredService<SyncJournal>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SqliteProductRepository>.Instance);

        Assert.Equal(before, (await products.GetAllAsync()).Count);
        reopened.Dispose();
    }

    // Demo stock: on hand = received - shipped. One product is at zero (Prince), one has no movements, one is low
    [Theory]
    [InlineData("3017620422003", 36)]
    [InlineData("4008400402222", 28)]
    [InlineData("5449000000996", 144)]
    [InlineData("5449000011527", 84)]
    [InlineData("8076809513326", 18)]
    [InlineData("7622210449283", 0)]
    [InlineData("3168930010265", 8)]
    [InlineData("8000500310427", 42)]
    [InlineData("5000159461122", 120)]
    [InlineData("3033490004743", 42)]
    [InlineData("9002490100070", 56)]
    [InlineData("5000157024671", 0)]
    public async Task Seed_QuantityOnHandMatchesTheDemoStock(string barcode, int expected)
    {
        var product = await _products.GetByBarcodeAsync(barcode);
        Assert.NotNull(product);

        var stock = await _provider.GetRequiredService<IStockService>().GetQuantityOnHandAsync(product.Id);

        Assert.Equal(expected, stock);
    }

    [Fact]
    public async Task Seed_ProductWithoutMovements_HasNoOperations()
    {
        var product = await _products.GetByBarcodeAsync("5000157024671");

        Assert.False(await _provider.GetRequiredService<IOperationRepository>().AnyForProductAsync(product!.Id));
    }
}
