using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// In-memory repositories with the real services built on them, wired the same way as the
// container. ViewModel tests share one store, so a change made by one service is seen by the others.
public sealed class TestStore
{
    public FakeClock Clock { get; } = new();
    public CurrentUserService CurrentUser { get; } = new();
    public FakeNavigationService Navigation { get; } = new();

    public InMemoryUserRepository Users { get; } = new();
    public InMemoryProductRepository Products { get; } = new();
    public InMemoryCharacteristicRepository Characteristics { get; } = new();
    public InMemoryProductCharacteristicRepository Values { get; } = new();
    public InMemoryOperationRepository Operations { get; } = new();

    // The services are stateless wrappers over the repositories above, so building them on access is cheap
    public IStockService Stock => new StockService(Operations, Products, CurrentUser, Clock);

    public ICharacteristicValidator Validator => new CharacteristicValidator(Characteristics);

    public ICharacteristicService CharacteristicService =>
        new CharacteristicService(Characteristics, Values, Products, Validator, CurrentUser, Clock);

    public IProductCatalog Catalog => new ProductCatalog(Products, Characteristics, Values, Operations, Users, Stock);

    // Adds the user to the user repository (for history names) and signs them in
    public async Task<User> SignInAsync(Role role, string fullName = "Test User")
    {
        var user = new User { FullName = fullName, Role = role };
        await Users.AddAsync(user);
        CurrentUser.SignIn(user);
        return user;
    }

    public async Task<Product> AddProductAsync(string name, string barcode)
    {
        var product = new Product { Name = name, Barcode = barcode };
        await Products.AddAsync(product);
        return product;
    }

    public async Task<Characteristic> AddCharacteristicAsync(string name, CharacteristicValueType valueType)
    {
        var characteristic = new Characteristic { Name = name, ValueType = valueType };
        await Characteristics.AddAsync(characteristic);
        return characteristic;
    }

    public Task SetValueAsync(Product product, Characteristic characteristic, string value) =>
        Values.AddAsync(new ProductCharacteristic
        {
            ProductId = product.Id,
            CharacteristicId = characteristic.Id,
            Value = value
        });
}
