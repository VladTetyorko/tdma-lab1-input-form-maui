using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using SQLite;

namespace Mobile_apps_L1_Model_view.Tests.Data;

// The SQLite repositories: every field survives a round trip, and every write queues exactly one change
public class SqliteRepositoryTests : IDisposable
{
    private readonly ServiceProvider _provider = new ServiceCollection().AddStockRoomCore().AddFakePlatform()
        .BuildServiceProvider();

    private IProductRepository Products => _provider.GetRequiredService<IProductRepository>();
    private IOperationRepository Operations => _provider.GetRequiredService<IOperationRepository>();
    private ICharacteristicRepository Characteristics => _provider.GetRequiredService<ICharacteristicRepository>();
    private IProductCharacteristicRepository Values => _provider.GetRequiredService<IProductCharacteristicRepository>();
    private IUserRepository Users => _provider.GetRequiredService<IUserRepository>();
    private ISyncQueue Queue => _provider.GetRequiredService<ISyncQueue>();

    public void Dispose() => _provider.Dispose();

    // Ticks beyond the second, so a lost precision would show
    private static readonly DateTime Created = new DateTime(2026, 1, 31, 10, 15, 0, DateTimeKind.Utc).AddTicks(1_234_567);

    private async Task<Product> AddProductAsync(string name = "Saw", string barcode = "4006381333931")
    {
        var product = new Product
        {
            Name = name, Barcode = barcode, CreatedByUserId = SeedData.ManagerId, CreatedAt = Created, UpdatedAt = Created
        };
        await Products.AddAsync(product);
        return product;
    }

    private async Task ClearQueueAsync()
    {
        foreach (var item in await Queue.GetPendingAsync())
        {
            await Queue.RemoveAsync(item.Id);
        }
    }

    [Fact]
    public async Task Product_RoundTrip_PreservesEveryField()
    {
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = "Cordless Saw  ",
            Barcode = "4006381333931",
            CreatedByUserId = SeedData.ManagerId,
            CreatedAt = Created,
            UpdatedAt = Created.AddHours(1)
        };
        await Products.AddAsync(product);

        var back = await Products.GetByIdAsync(product.Id);

        Assert.NotNull(back);
        Assert.Equal(product.Id, back.Id);
        Assert.Equal(product.Name, back.Name);
        Assert.Equal(product.Barcode, back.Barcode);
        Assert.Equal(product.CreatedByUserId, back.CreatedByUserId);
        Assert.Equal(Created.Ticks, back.CreatedAt.Ticks);
        Assert.Equal(Created.AddHours(1).Ticks, back.UpdatedAt.Ticks);
        Assert.Equal(DateTimeKind.Utc, back.CreatedAt.Kind);
    }

    [Fact]
    public async Task Product_Update_IsReadBack()
    {
        var product = await AddProductAsync();

        product.Name = "Circular Saw";
        product.UpdatedAt = Created.AddMinutes(5);
        await Products.UpdateAsync(product);

        var back = await Products.GetByIdAsync(product.Id);
        Assert.Equal("Circular Saw", back!.Name);
        Assert.Equal(Created.AddMinutes(5).Ticks, back.UpdatedAt.Ticks);
        Assert.Equal(Created.Ticks, back.CreatedAt.Ticks);
    }

    [Fact]
    public async Task Product_GetByBarcode_MatchesExactly()
    {
        await AddProductAsync(barcode: "4006381333931");

        Assert.NotNull(await Products.GetByBarcodeAsync("4006381333931"));
        Assert.Null(await Products.GetByBarcodeAsync("400638133393"));
        Assert.Null(await Products.GetByBarcodeAsync("4006381333931 "));
    }

    [Fact]
    public async Task Operation_RoundTrip_PreservesEnumsNumbersAndNulls()
    {
        var product = await AddProductAsync();
        var operation = new Operation
        {
            Id = Guid.CreateVersion7(),
            ProductId = product.Id,
            Direction = OperationDirection.Out,
            Quantity = 7,
            Counterparty = null,
            Reason = "Delivered to site",
            CreatedByUserId = SeedData.StorekeeperId,
            CreatedAt = Created,
            UpdatedAt = Created
        };
        await Operations.AddAsync(operation);

        var back = Assert.Single(await Operations.GetByProductAsync(product.Id));

        Assert.Equal(operation.Id, back.Id);
        Assert.Equal(OperationDirection.Out, back.Direction);
        Assert.Equal(7, back.Quantity);
        Assert.Null(back.Counterparty);
        Assert.Equal("Delivered to site", back.Reason);
        Assert.Equal(SeedData.StorekeeperId, back.CreatedByUserId);
        Assert.Equal(Created.Ticks, back.CreatedAt.Ticks);
    }

    [Fact]
    public async Task Operation_WithCounterpartyButNoReason_ReadsBackBothCorrectly()
    {
        var product = await AddProductAsync();
        await Operations.AddAsync(new Operation
        {
            ProductId = product.Id, Direction = OperationDirection.In, Quantity = 2,
            Counterparty = "Acme Ltd, Kyiv", Reason = null, CreatedByUserId = SeedData.DriverId
        });

        var back = Assert.Single(await Operations.GetByProductAsync(product.Id));

        Assert.Equal("Acme Ltd, Kyiv", back.Counterparty);
        Assert.Null(back.Reason);
    }

    [Fact]
    public async Task Characteristic_RoundTrip_KeepsValueType()
    {
        var characteristic = new Characteristic
        {
            Name = "Length (mm)", ValueType = CharacteristicValueType.Number,
            CreatedByUserId = SeedData.ManagerId, CreatedAt = Created, UpdatedAt = Created
        };
        await Characteristics.AddAsync(characteristic);

        var back = (await Characteristics.GetAllAsync()).Single(c => c.Id == characteristic.Id);

        Assert.Equal("Length (mm)", back.Name);
        Assert.Equal(CharacteristicValueType.Number, back.ValueType);
        Assert.Equal(SeedData.ManagerId, back.CreatedByUserId);
        Assert.Equal(Created.Ticks, back.CreatedAt.Ticks);
    }

    [Fact]
    public async Task ProductCharacteristic_RoundTrip_KeepsTheValueText()
    {
        var product = await AddProductAsync();
        var characteristic = (await Characteristics.GetAllAsync())[0];
        var value = new ProductCharacteristic
        {
            ProductId = product.Id, CharacteristicId = characteristic.Id, Value = "Ünïcode 12,5 kg",
            CreatedByUserId = SeedData.ManagerId, CreatedAt = Created, UpdatedAt = Created
        };
        await Values.AddAsync(value);

        var back = Assert.Single(await Values.GetByProductAsync(product.Id));

        Assert.Equal(value.Id, back.Id);
        Assert.Equal("Ünïcode 12,5 kg", back.Value);
        Assert.Equal(characteristic.Id, back.CharacteristicId);
    }

    [Fact]
    public async Task User_RoundTrip_KeepsRoleAndName()
    {
        var driver = await Users.GetByIdAsync(SeedData.DriverId);

        Assert.NotNull(driver);
        Assert.Equal(Role.Driver, driver.Role);
        Assert.False(string.IsNullOrWhiteSpace(driver.FullName));
        Assert.Equal(DateTimeKind.Utc, driver.CreatedAt.Kind);
    }

    [Fact]
    public async Task AddingAProduct_QueuesOneInsert()
    {
        var product = await AddProductAsync();

        var item = Assert.Single(await Queue.GetPendingAsync());
        Assert.Equal("Products", item.EntityType);
        Assert.Equal(product.Id, item.EntityId);
        Assert.Equal(SyncOperation.Insert, item.Operation);
        Assert.Contains("\"Name\":\"Saw\"", item.PayloadJson);
        Assert.Equal(Created.Ticks, item.CreatedAt.Ticks);
    }

    [Fact]
    public async Task UpdatingAProduct_QueuesOneUpdate_StampedWithTheChangeTime()
    {
        var product = await AddProductAsync();
        await ClearQueueAsync();

        product.Name = "Circular Saw";
        product.UpdatedAt = Created.AddMinutes(5);
        await Products.UpdateAsync(product);

        var item = Assert.Single(await Queue.GetPendingAsync());
        Assert.Equal(SyncOperation.Update, item.Operation);
        Assert.Equal(product.Id, item.EntityId);
        Assert.Contains("Circular Saw", item.PayloadJson);
        Assert.Equal(Created.AddMinutes(5).Ticks, item.CreatedAt.Ticks);
    }

    [Fact]
    public async Task DeletingAProductWithoutValues_QueuesOneDelete_WithTheRemovedRow()
    {
        var product = await AddProductAsync("Hammer");
        await ClearQueueAsync();

        await Products.DeleteAsync(product.Id);

        var item = Assert.Single(await Queue.GetPendingAsync());
        Assert.Equal(("Products", SyncOperation.Delete), (item.EntityType, item.Operation));
        Assert.Equal(product.Id, item.EntityId);
        Assert.Contains("Hammer", item.PayloadJson);
        Assert.Null(await Products.GetByIdAsync(product.Id));
    }

    [Fact]
    public async Task DeletingAProductWithValues_QueuesEachValueThenTheProduct()
    {
        var product = await AddProductAsync();
        var characteristics = await Characteristics.GetAllAsync();
        var first = new ProductCharacteristic
        {
            ProductId = product.Id, CharacteristicId = characteristics[0].Id, Value = "red",
            CreatedByUserId = SeedData.ManagerId, CreatedAt = Created, UpdatedAt = Created
        };
        var second = new ProductCharacteristic
        {
            ProductId = product.Id, CharacteristicId = characteristics[1].Id, Value = "2",
            CreatedByUserId = SeedData.ManagerId, CreatedAt = Created, UpdatedAt = Created
        };
        await Values.AddAsync(first);
        await Values.AddAsync(second);
        await ClearQueueAsync();

        await Products.DeleteAsync(product.Id);

        var items = await Queue.GetPendingAsync();
        Assert.Equal(["ProductCharacteristics", "ProductCharacteristics", "Products"], items.Select(i => i.EntityType));
        Assert.All(items, i => Assert.Equal(SyncOperation.Delete, i.Operation));
        Assert.Equal(new[] { first.Id, second.Id, product.Id }, items.Select(i => i.EntityId));
        Assert.Empty(await Values.GetByProductAsync(product.Id));
    }

    [Fact]
    public async Task AddingAnOperation_QueuesOneInsert_WithTheCounterpartyEncrypted()
    {
        var product = await AddProductAsync();
        await ClearQueueAsync();

        var operation = new Operation
        {
            ProductId = product.Id, Direction = OperationDirection.In, Quantity = 4,
            Counterparty = "Acme Ltd", CreatedByUserId = SeedData.DriverId
        };
        await Operations.AddAsync(operation);

        var item = Assert.Single(await Queue.GetPendingAsync());
        Assert.Equal(("Operations", SyncOperation.Insert), (item.EntityType, item.Operation));
        Assert.Equal(operation.Id, item.EntityId);
        Assert.Contains(operation.Id.ToString(), item.PayloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Acme", item.PayloadJson);
    }

    [Fact]
    public async Task AddingACharacteristic_QueuesOneInsert()
    {
        await ClearQueueAsync();
        var characteristic = new Characteristic
        {
            Name = "Shelf life", ValueType = CharacteristicValueType.Text, CreatedByUserId = SeedData.ManagerId
        };

        await Characteristics.AddAsync(characteristic);

        var item = Assert.Single(await Queue.GetPendingAsync());
        Assert.Equal(("Characteristics", SyncOperation.Insert), (item.EntityType, item.Operation));
        Assert.Equal(characteristic.Id, item.EntityId);
    }

    [Fact]
    public async Task AnyForProduct_IsFalseForANewProduct_AndTrueOnceAnOperationExists()
    {
        var product = await AddProductAsync();
        Assert.False(await Operations.AnyForProductAsync(product.Id));

        await Operations.AddAsync(new Operation
        {
            ProductId = product.Id, Direction = OperationDirection.In, Quantity = 1, CreatedByUserId = SeedData.DriverId
        });

        Assert.True(await Operations.AnyForProductAsync(product.Id));
    }

    [Fact]
    public async Task AnyForProduct_IsTrueForASeededProductWithHistory()
    {
        var anyOperation = (await Operations.GetAllAsync())[0];

        Assert.True(await Operations.AnyForProductAsync(anyOperation.ProductId));
    }

    [Fact]
    public async Task DeletingAMissingProduct_IsNotAnError_AndQueuesNothing()
    {
        await Products.DeleteAsync(Guid.NewGuid());

        Assert.Equal(0, await Queue.CountAsync());
    }

    [Fact]
    public async Task UpdatingAMissingProduct_Fails_AndQueuesNothing()
    {
        var ghost = new Product { Id = Guid.NewGuid(), Name = "Ghost", Barcode = "4006381333931", CreatedByUserId = SeedData.ManagerId };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Products.UpdateAsync(ghost));

        Assert.Contains("does not exist", ex.Message);
        Assert.Equal(0, await Queue.CountAsync());
    }

    [Fact]
    public async Task Counterparty_WhenTheKeyIsGone_ShowsThePlaceholderAndDoesNotThrow()
    {
        var product = await AddProductAsync();
        await Operations.AddAsync(new Operation
        {
            ProductId = product.Id, Direction = OperationDirection.In, Quantity = 1,
            Counterparty = "Acme Ltd", CreatedByUserId = SeedData.DriverId
        });
        _provider.GetRequiredService<FakeSecretStore>().Values.Clear();

        var repository = Reopen(new AesFieldEncryptor(_provider.GetRequiredService<FakeSecretStore>()));
        var history = await repository.GetByProductAsync(product.Id);

        Assert.Equal(SqliteOperationRepository.UnreadableCounterparty, Assert.Single(history).Counterparty);
    }

    [Fact]
    public async Task Counterparty_WhenSecureStorageFails_ShowsThePlaceholderAndDoesNotThrow()
    {
        var product = await AddProductAsync();
        await Operations.AddAsync(new Operation
        {
            ProductId = product.Id, Direction = OperationDirection.In, Quantity = 1,
            Counterparty = "Acme Ltd", CreatedByUserId = SeedData.DriverId
        });
        var secrets = _provider.GetRequiredService<FakeSecretStore>();
        secrets.Failing = true;

        var history = await Reopen(new AesFieldEncryptor(secrets)).GetByProductAsync(product.Id);

        Assert.Equal(SqliteOperationRepository.UnreadableCounterparty, Assert.Single(history).Counterparty);
    }

    [Fact]
    public async Task NoCounterparty_IsStoredAsNull_NotAsEncryptedText()
    {
        var product = await AddProductAsync();
        var operation = new Operation
        {
            ProductId = product.Id, Direction = OperationDirection.In, Quantity = 1, CreatedByUserId = SeedData.DriverId
        };
        await Operations.AddAsync(operation);

        var stored = await _provider.GetRequiredService<IDatabase>().GetConnectionAsync();
        var raw = await stored.ExecuteScalarAsync<string?>(
            "SELECT Counterparty FROM Operations WHERE Id = ?", operation.Id.ToString());

        Assert.Null(raw);
    }

    private SqliteOperationRepository Reopen(IFieldEncryptor encryptor) => new(
        _provider.GetRequiredService<IDatabase>(),
        _provider.GetRequiredService<SyncJournal>(),
        encryptor,
        NullLogger<SqliteOperationRepository>.Instance);
}
