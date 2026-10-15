using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ProductServiceTests
{
    private const string ValidBarcode = "4006381333931";

    private readonly InMemoryProductRepository _products = new();
    private readonly InMemoryOperationRepository _operations = new();
    private readonly CurrentUserService _currentUser = new();
    private readonly FakeClock _clock = new();
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _service = new ProductService(_products, _operations, new ProductValidator(_products), _currentUser, _clock);
    }

    [Fact]
    public async Task AddAsync_NotSignedIn_Fails()
    {
        var result = await _service.AddAsync("Cordless Saw", ValidBarcode);

        Assert.False(result.IsSuccess);
        Assert.Equal("Sign in before adding products.", result.Error);
        Assert.Empty(await _products.GetAllAsync());
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task AddAsync_NonManager_Fails(Role role)
    {
        _currentUser.SignIn(new User { FullName = "Petro Bondarenko", Role = role });

        var result = await _service.AddAsync("Cordless Saw", ValidBarcode);

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot add products", result.Error);
        Assert.Empty(await _products.GetAllAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddAsync_BlankName_Fails(string? name)
    {
        SignInAsManager();

        var result = await _service.AddAsync(name, ValidBarcode);

        Assert.False(result.IsSuccess);
        Assert.Equal("Name is required.", result.Error);
        Assert.Empty(await _products.GetAllAsync());
    }

    [Fact]
    public async Task AddAsync_InvalidBarcode_Fails()
    {
        SignInAsManager();

        var result = await _service.AddAsync("Cordless Saw", "4006381333932");

        Assert.False(result.IsSuccess);
        Assert.Contains("valid EAN-13", result.Error);
        Assert.Empty(await _products.GetAllAsync());
    }

    [Fact]
    public async Task AddAsync_DuplicateBarcode_Fails()
    {
        SignInAsManager();
        await _products.AddAsync(new Product { Name = "Drill", Barcode = ValidBarcode });

        var result = await _service.AddAsync("Cordless Saw", ValidBarcode);

        Assert.False(result.IsSuccess);
        Assert.Equal("A product with this barcode already exists.", result.Error);
        Assert.Single(await _products.GetAllAsync());
    }

    [Fact]
    public async Task AddAsync_Valid_TrimsInputAndStoresProductOwnedByCurrentUser()
    {
        var manager = SignInAsManager();

        var result = await _service.AddAsync("  Cordless Saw  ", " " + ValidBarcode + " ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Cordless Saw", result.Value!.Name);
        Assert.Equal(ValidBarcode, result.Value.Barcode);
        Assert.Equal(manager.Id, result.Value.CreatedByUserId);

        var stored = await _products.GetByBarcodeAsync(ValidBarcode);
        Assert.NotNull(stored);
        Assert.Equal(result.Value.Id, stored.Id);
    }

    [Fact]
    public async Task AddAsync_Valid_SetsTimestampsFromClock()
    {
        SignInAsManager();
        _clock.UtcNow = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

        var result = await _service.AddAsync("Cordless Saw", ValidBarcode);

        Assert.Equal(_clock.UtcNow, result.Value!.CreatedAt);
        Assert.Equal(_clock.UtcNow, result.Value.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_ChangesNameAndKeepsTheOwnBarcode()
    {
        SignInAsManager();
        var saw = (await _service.AddAsync("Cordless Saw", ValidBarcode)).Value!;
        _clock.UtcNow = saw.UpdatedAt.AddMinutes(5);

        var result = await _service.UpdateAsync(saw.Id, " Circular Saw ", ValidBarcode);

        Assert.True(result.IsSuccess);
        var stored = await _products.GetByIdAsync(saw.Id);
        Assert.Equal("Circular Saw", stored!.Name);
        Assert.Equal(_clock.UtcNow, stored.UpdatedAt);
        Assert.Equal(saw.CreatedAt, stored.CreatedAt);
    }

    [Fact]
    public async Task UpdateAsync_BarcodeOfAnotherProduct_Fails()
    {
        SignInAsManager();
        await _service.AddAsync("Cordless Saw", ValidBarcode);
        var drill = (await _service.AddAsync("Drill", "4820001000016")).Value!;

        var result = await _service.UpdateAsync(drill.Id, "Drill", ValidBarcode);

        Assert.False(result.IsSuccess);
        Assert.Equal("A product with this barcode already exists.", result.Error);
        Assert.Equal("4820001000016", (await _products.GetByIdAsync(drill.Id))!.Barcode);
    }

    [Fact]
    public async Task UpdateAsync_NonManager_Fails()
    {
        _currentUser.SignIn(new User { FullName = "Petro", Role = Role.Storekeeper });

        var result = await _service.UpdateAsync(Guid.NewGuid(), "Saw", ValidBarcode);

        Assert.Contains("cannot edit products", result.Error);
    }

    [Fact]
    public async Task DeleteAsync_ProductWithoutOperations_RemovesIt()
    {
        SignInAsManager();
        var saw = (await _service.AddAsync("Cordless Saw", ValidBarcode)).Value!;

        var result = await _service.DeleteAsync(saw.Id);

        Assert.True(result.IsSuccess);
        Assert.Null(await _products.GetByIdAsync(saw.Id));
    }

    [Fact]
    public async Task DeleteAsync_ProductWithOperations_IsRefusedWithAnExplanation()
    {
        SignInAsManager();
        var saw = (await _service.AddAsync("Cordless Saw", ValidBarcode)).Value!;
        await _operations.AddAsync(new Operation { ProductId = saw.Id, Direction = OperationDirection.In, Quantity = 1 });

        var result = await _service.DeleteAsync(saw.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductService.HasOperationsMessage, result.Error);
        Assert.NotNull(await _products.GetByIdAsync(saw.Id));
    }

    [Fact]
    public async Task DeleteAsync_NonManager_Fails()
    {
        _currentUser.SignIn(new User { FullName = "Ivan", Role = Role.Driver });

        var result = await _service.DeleteAsync(Guid.NewGuid());

        Assert.Contains("cannot delete products", result.Error);
    }

    private User SignInAsManager()
    {
        var manager = new User { FullName = "Olena Kovalenko", Role = Role.Manager };
        _currentUser.SignIn(manager);
        return manager;
    }
}
