using System.Globalization;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Data;

// Demo data: 3 users (one per role), 10 products with valid EAN-13 codes,
// 4 characteristics with values, and some stock movements
public sealed class DataSeeder(
    IUserRepository users,
    IProductRepository products,
    ICharacteristicRepository characteristics,
    IProductCharacteristicRepository values,
    IOperationRepository operations) : IDataSeeder
{
    private sealed record Seed(string Name, string BarcodeBase, double WeightKg, string Color, string Category,
        double VolumeLitres, int Received, int Shipped);

    // Fixed ids: the saved session (IAppStateService) names the signed-in user by id, and the
    // in-memory data is rebuilt on every start. Lab 4 seeds the same ids in its migration.
    public static readonly Guid ManagerId = Guid.Parse("0196a000-0000-7000-8000-000000000001");
    public static readonly Guid StorekeeperId = Guid.Parse("0196a000-0000-7000-8000-000000000002");
    public static readonly Guid DriverId = Guid.Parse("0196a000-0000-7000-8000-000000000003");

    private static readonly Seed[] ProductSeeds =
    [
        new("Cordless Drill 18V", "482000100001", 1.6, "Blue", "Tools", 3.2, 25, 4),
        new("Claw Hammer 500 g", "482000100002", 0.5, "Black", "Tools", 0.6, 40, 12),
        new("Work Gloves, pair", "482000100003", 0.2, "Yellow", "Safety", 0.8, 120, 35),
        new("Safety Helmet", "482000100004", 0.4, "White", "Safety", 5.0, 30, 6),
        new("Interior Paint 10 L", "482000100005", 12.5, "White", "Paint", 10.0, 18, 5),
        new("Wood Screws 4x40, box", "482000100006", 1.0, "Silver", "Fasteners", 0.7, 200, 80),
        new("Extension Cord 5 m", "482000100007", 0.9, "Orange", "Electrical", 1.5, 15, 15),
        new("LED Bulb E27 10 W", "482000100008", 0.1, "Warm white", "Electrical", 0.2, 150, 60),
        new("Steel Tape Measure 5 m", "482000100009", 0.3, "Yellow", "Tools", 0.3, 50, 8),
        new("Nitrile Gloves, box", "482000100010", 0.4, "Blue", "Safety", 1.2, 0, 0)
    ];

    public async Task SeedAsync()
    {
        if ((await users.GetAllAsync()).Count > 0)
        {
            return;
        }

        var manager = await AddUserAsync(ManagerId, "Olena Kovalenko", Role.Manager);
        var storekeeper = await AddUserAsync(StorekeeperId, "Petro Bondarenko", Role.Storekeeper);
        var driver = await AddUserAsync(DriverId, "Ivan Shevchenko", Role.Driver);

        var weight = await AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number, manager);
        var volume = await AddCharacteristicAsync("Volume (l)", CharacteristicValueType.Number, manager);
        var color = await AddCharacteristicAsync("Color", CharacteristicValueType.Text, manager);
        var category = await AddCharacteristicAsync("Category", CharacteristicValueType.Text, manager);

        foreach (var seed in ProductSeeds)
        {
            var product = new Product
            {
                Name = seed.Name,
                Barcode = Ean13.WithCheckDigit(seed.BarcodeBase),
                CreatedByUserId = manager.Id
            };
            await products.AddAsync(product);

            await AddValueAsync(product, weight, FormatNumber(seed.WeightKg), manager);
            await AddValueAsync(product, volume, FormatNumber(seed.VolumeLitres), manager);
            await AddValueAsync(product, color, seed.Color, manager);
            await AddValueAsync(product, category, seed.Category, manager);

            if (seed.Received > 0)
            {
                await AddOperationAsync(product, OperationDirection.In, seed.Received, driver, "Initial delivery");
            }

            if (seed.Shipped > 0)
            {
                await AddOperationAsync(product, OperationDirection.Out, seed.Shipped, storekeeper, "Issued to site");
            }
        }
    }

    // Invariant culture: values are stored as text and parsed back as numbers later
    private static string FormatNumber(double number) => number.ToString(CultureInfo.InvariantCulture);

    private async Task<User> AddUserAsync(Guid id, string fullName, Role role)
    {
        var user = new User { Id = id, FullName = fullName, Role = role };
        await users.AddAsync(user);
        return user;
    }

    private async Task<Characteristic> AddCharacteristicAsync(string name, CharacteristicValueType type, User owner)
    {
        var characteristic = new Characteristic { Name = name, ValueType = type, CreatedByUserId = owner.Id };
        await characteristics.AddAsync(characteristic);
        return characteristic;
    }

    private Task AddValueAsync(Product product, Characteristic characteristic, string value, User owner) =>
        values.AddAsync(new ProductCharacteristic
        {
            ProductId = product.Id,
            CharacteristicId = characteristic.Id,
            Value = value,
            CreatedByUserId = owner.Id
        });

    private Task AddOperationAsync(Product product, OperationDirection direction, int quantity, User owner,
        string reason) =>
        operations.AddAsync(new Operation
        {
            ProductId = product.Id,
            Direction = direction,
            Quantity = quantity,
            Reason = reason,
            CreatedByUserId = owner.Id
        });
}
