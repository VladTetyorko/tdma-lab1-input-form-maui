using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class StockServiceTests
{
    private readonly InMemoryOperationRepository _operations = new();
    private readonly InMemoryProductRepository _products = new();
    private readonly CurrentUserService _currentUser = new();
    private readonly FakeClock _clock = new();
    private readonly StockService _service;

    public StockServiceTests()
    {
        _service = new StockService(_operations, _products, _currentUser, _clock);
    }

    [Fact]
    public async Task GetQuantityOnHandAsync_IsInMinusOut()
    {
        var product = await AddProductAsync("Drill");
        await AddOperationAsync(product, OperationDirection.In, 10);
        await AddOperationAsync(product, OperationDirection.Out, 4);

        Assert.Equal(6, await _service.GetQuantityOnHandAsync(product.Id));
    }

    [Fact]
    public async Task GetQuantityOnHandAsync_IsZeroWithoutOperations()
    {
        var product = await AddProductAsync("Drill");

        Assert.Equal(0, await _service.GetQuantityOnHandAsync(product.Id));
    }

    [Fact]
    public async Task GetQuantitiesAsync_ReturnsQuantityPerProductWithOperations()
    {
        var drill = await AddProductAsync("Drill");
        var saw = await AddProductAsync("Saw");
        await AddOperationAsync(drill, OperationDirection.In, 10);
        await AddOperationAsync(drill, OperationDirection.Out, 3);
        await AddOperationAsync(saw, OperationDirection.In, 5);

        var quantities = await _service.GetQuantitiesAsync();

        Assert.Equal(7, quantities[drill.Id]);
        Assert.Equal(5, quantities[saw.Id]);
    }

    [Fact]
    public async Task GetQuantitiesAsync_OmitsProductsWithoutOperations()
    {
        var product = await AddProductAsync("Drill");

        var quantities = await _service.GetQuantitiesAsync();

        Assert.False(quantities.ContainsKey(product.Id));
    }

    [Fact]
    public async Task RecordAsync_NotSignedIn_Fails()
    {
        var product = await AddProductAsync("Drill");

        var result = await _service.RecordAsync(product.Id, OperationDirection.In, 5);

        Assert.False(result.IsSuccess);
        Assert.Equal("Sign in before recording operations.", result.Error);
        Assert.Empty(await _operations.GetAllAsync());
    }

    [Fact]
    public async Task RecordAsync_DriverOut_Fails()
    {
        var product = await AddProductAsync("Drill");
        await AddOperationAsync(product, OperationDirection.In, 10);
        SignIn(Role.Driver);

        var result = await _service.RecordAsync(product.Id, OperationDirection.Out, 3);

        Assert.False(result.IsSuccess);
        Assert.Equal("Your role (Driver) cannot record Out operations.", result.Error);
        Assert.Equal(10, await _service.GetQuantityOnHandAsync(product.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task RecordAsync_QuantityNotPositive_Fails(int quantity)
    {
        var product = await AddProductAsync("Drill");
        SignIn(Role.Storekeeper);

        var result = await _service.RecordAsync(product.Id, OperationDirection.In, quantity);

        Assert.False(result.IsSuccess);
        Assert.Equal("Quantity must be greater than zero.", result.Error);
        Assert.Empty(await _operations.GetAllAsync());
    }

    [Fact]
    public async Task RecordAsync_UnknownProduct_Fails()
    {
        SignIn(Role.Storekeeper);

        var result = await _service.RecordAsync(Guid.NewGuid(), OperationDirection.In, 5);

        Assert.False(result.IsSuccess);
        Assert.Equal("Product not found.", result.Error);
        Assert.Empty(await _operations.GetAllAsync());
    }

    [Fact]
    public async Task RecordAsync_OutMoreThanOnHand_Fails()
    {
        var product = await AddProductAsync("Drill");
        await AddOperationAsync(product, OperationDirection.In, 2);
        SignIn(Role.Storekeeper);

        var result = await _service.RecordAsync(product.Id, OperationDirection.Out, 3);

        Assert.False(result.IsSuccess);
        Assert.Equal("Not enough units on hand.", result.Error);
        Assert.Equal(2, await _service.GetQuantityOnHandAsync(product.Id));
    }

    [Fact]
    public async Task RecordAsync_OutEqualToOnHand_Succeeds()
    {
        var product = await AddProductAsync("Drill");
        await AddOperationAsync(product, OperationDirection.In, 2);
        SignIn(Role.Storekeeper);

        var result = await _service.RecordAsync(product.Id, OperationDirection.Out, 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, await _service.GetQuantityOnHandAsync(product.Id));
    }

    [Fact]
    public async Task RecordAsync_Valid_StoresOperationOwnedByCurrentUserWithClockTimestamps()
    {
        var product = await AddProductAsync("Drill");
        var driver = SignIn(Role.Driver);
        _clock.UtcNow = new DateTime(2026, 5, 20, 8, 15, 0, DateTimeKind.Utc);

        var result = await _service.RecordAsync(product.Id, OperationDirection.In, 5,
            counterparty: "Supplier A", reason: "Delivery");

        Assert.True(result.IsSuccess);
        var operation = result.Value!;
        Assert.Equal(product.Id, operation.ProductId);
        Assert.Equal(OperationDirection.In, operation.Direction);
        Assert.Equal(5, operation.Quantity);
        Assert.Equal("Supplier A", operation.Counterparty);
        Assert.Equal("Delivery", operation.Reason);
        Assert.Equal(driver.Id, operation.CreatedByUserId);
        Assert.Equal(_clock.UtcNow, operation.CreatedAt);
        Assert.Equal(_clock.UtcNow, operation.UpdatedAt);

        var stored = await _operations.GetByProductAsync(product.Id);
        Assert.Equal(operation.Id, Assert.Single(stored).Id);
        Assert.Equal(5, await _service.GetQuantityOnHandAsync(product.Id));
    }

    private User SignIn(Role role)
    {
        var user = new User { FullName = $"Test {role}", Role = role };
        _currentUser.SignIn(user);
        return user;
    }

    private async Task<Product> AddProductAsync(string name)
    {
        var product = new Product { Name = name, Barcode = "4006381333931" };
        await _products.AddAsync(product);
        return product;
    }

    private Task AddOperationAsync(Product product, OperationDirection direction, int quantity) =>
        _operations.AddAsync(new Operation
        {
            ProductId = product.Id,
            Direction = direction,
            Quantity = quantity
        });
}
