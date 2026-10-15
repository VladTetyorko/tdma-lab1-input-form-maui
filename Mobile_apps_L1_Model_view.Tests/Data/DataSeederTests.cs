using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Data;

public class DataSeederTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryProductRepository _products = new();
    private readonly InMemoryCharacteristicRepository _characteristics = new();
    private readonly InMemoryProductCharacteristicRepository _values = new();
    private readonly InMemoryOperationRepository _operations = new();
    private readonly DataSeeder _seeder;

    public DataSeederTests()
    {
        _seeder = new DataSeeder(_users, _products, _characteristics, _values, _operations);
    }

    [Fact]
    public async Task SeedAsync_CreatesOneUserPerRole()
    {
        await _seeder.SeedAsync();

        var users = await _users.GetAllAsync();
        Assert.Equal(3, users.Count);
        Assert.Equal(
            [Role.Storekeeper, Role.Driver, Role.Manager],
            users.Select(u => u.Role).OrderBy(r => r).ToArray());
    }

    [Fact]
    public async Task SeedAsync_CreatesFourCharacteristics()
    {
        await _seeder.SeedAsync();

        Assert.Equal(4, (await _characteristics.GetAllAsync()).Count);
    }

    [Fact]
    public async Task SeedAsync_CreatesTenProductsWithValidAndUniqueBarcodes()
    {
        await _seeder.SeedAsync();

        var products = await _products.GetAllAsync();
        Assert.Equal(10, products.Count);
        Assert.All(products, p => Assert.True(Ean13.IsValid(p.Barcode), $"Invalid barcode for {p.Name}"));
        Assert.Equal(10, products.Select(p => p.Barcode).Distinct().Count());
    }

    [Fact]
    public async Task SeedAsync_GivesEveryProductCharacteristicValues()
    {
        await _seeder.SeedAsync();

        foreach (var product in await _products.GetAllAsync())
        {
            Assert.Equal(4, (await _values.GetByProductAsync(product.Id)).Count);
        }
    }

    [Fact]
    public async Task SeedAsync_OperationsHavePositiveQuantityAndExistingProduct()
    {
        await _seeder.SeedAsync();

        var operations = await _operations.GetAllAsync();
        Assert.NotEmpty(operations);
        foreach (var operation in operations)
        {
            Assert.True(operation.Quantity > 0, $"Quantity must be positive for operation {operation.Id}");
            Assert.NotNull(await _products.GetByIdAsync(operation.ProductId));
        }
    }

    [Fact]
    public async Task SeedAsync_SeedsStockForProductsWithMovements()
    {
        await _seeder.SeedAsync();

        var drill = await _products.GetByBarcodeAsync(Ean13.WithCheckDigit("482000100001"));
        Assert.NotNull(drill);
        var drillOperations = await _operations.GetByProductAsync(drill.Id);
        var onHand = drillOperations.Sum(o => o.Direction == OperationDirection.In ? o.Quantity : -o.Quantity);
        Assert.Equal(21, onHand);
    }

    [Fact]
    public async Task SeedAsync_CalledTwice_AddsNothingTheSecondTime()
    {
        await _seeder.SeedAsync();
        var usersBefore = (await _users.GetAllAsync()).Count;
        var productsBefore = (await _products.GetAllAsync()).Count;
        var operationsBefore = (await _operations.GetAllAsync()).Count;
        var characteristicsBefore = (await _characteristics.GetAllAsync()).Count;

        await _seeder.SeedAsync();

        Assert.Equal(usersBefore, (await _users.GetAllAsync()).Count);
        Assert.Equal(productsBefore, (await _products.GetAllAsync()).Count);
        Assert.Equal(operationsBefore, (await _operations.GetAllAsync()).Count);
        Assert.Equal(characteristicsBefore, (await _characteristics.GetAllAsync()).Count);
    }
}
