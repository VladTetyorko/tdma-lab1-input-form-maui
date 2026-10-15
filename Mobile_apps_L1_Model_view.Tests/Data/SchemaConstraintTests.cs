using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using SQLite;

namespace Mobile_apps_L1_Model_view.Tests.Data;

// The constraints of Migrations/0001_initial.sql, checked with raw SQL against a migrated temporary file
public class SchemaConstraintTests : IDisposable
{
    private const string Now = "2026-01-15T09:30:00.0000000Z";

    private readonly ServiceProvider _provider = new ServiceCollection().AddStockRoomCore().AddFakePlatform()
        .BuildServiceProvider();

    public void Dispose() => _provider.Dispose();

    private async Task<SQLiteAsyncConnection> Db() =>
        await _provider.GetRequiredService<IDatabase>().GetConnectionAsync();

    private static string NewId() => RowFormat.Text(Guid.CreateVersion7());

    private static string ManagerId => RowFormat.Text(SeedData.ManagerId);

    private async Task<string> ExistingBarcodeAsync() =>
        await (await Db()).ExecuteScalarAsync<string>("SELECT Barcode FROM Products ORDER BY rowid LIMIT 1");

    private async Task<string> ExistingCharacteristicNameAsync() =>
        await (await Db()).ExecuteScalarAsync<string>("SELECT Name FROM Characteristics ORDER BY rowid LIMIT 1");

    private async Task<string> ExistingProductIdAsync() =>
        await (await Db()).ExecuteScalarAsync<string>("SELECT Id FROM Products ORDER BY rowid LIMIT 1");

    private async Task InsertProductAsync(string barcode, string createdBy)
    {
        await (await Db()).ExecuteAsync(
            "INSERT INTO Products (Id, Name, Barcode, CreatedByUserId, CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, ?, ?)",
            NewId(), "Test", barcode, createdBy, Now, Now);
    }

    [Fact]
    public async Task Products_DuplicateBarcode_IsRejected()
    {
        var barcode = await ExistingBarcodeAsync();

        await Assert.ThrowsAsync<SQLiteException>(() => InsertProductAsync(barcode, ManagerId));
    }

    [Fact]
    public async Task Products_BlankName_IsRejected()
    {
        await Assert.ThrowsAsync<SQLiteException>(async () => await (await Db()).ExecuteAsync(
            "INSERT INTO Products (Id, Name, Barcode, CreatedByUserId, CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, ?, ?)",
            NewId(), "   ", "4006381333931", ManagerId, Now, Now));
    }

    [Fact]
    public async Task Products_UnknownCreator_IsRejectedByTheForeignKey()
    {
        await Assert.ThrowsAsync<SQLiteException>(() => InsertProductAsync("4006381333931", NewId()));
    }

    [Fact]
    public async Task Characteristics_NameDifferingOnlyByCase_IsRejected()
    {
        var name = await ExistingCharacteristicNameAsync();
        var otherCase = name.Any(char.IsLower) ? name.ToUpperInvariant() : name.ToLowerInvariant();

        var ex = await Assert.ThrowsAsync<SQLiteException>(async () => await (await Db()).ExecuteAsync(
            "INSERT INTO Characteristics (Id, Name, ValueType, CreatedByUserId, CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, ?, ?)",
            NewId(), otherCase, "Text", ManagerId, Now, Now));

        Assert.Contains("UNIQUE", ex.Message);
    }

    [Fact]
    public async Task ProductCharacteristics_SameProductAndCharacteristicTwice_IsRejected()
    {
        var connection = await Db();
        var pair = await connection.QueryAsync<PairRow>(
            "SELECT ProductId, CharacteristicId FROM ProductCharacteristics ORDER BY rowid LIMIT 1");
        var (productId, characteristicId) = (pair[0].ProductId, pair[0].CharacteristicId);

        await Assert.ThrowsAsync<SQLiteException>(() => connection.ExecuteAsync(
            "INSERT INTO ProductCharacteristics (Id, ProductId, CharacteristicId, Value, CreatedByUserId, CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, ?, ?, ?)",
            NewId(), productId, characteristicId, "again", ManagerId, Now, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Operations_QuantityNotPositive_IsRejected(int quantity)
    {
        var connection = await Db();
        var productId = await ExistingProductIdAsync();

        await Assert.ThrowsAsync<SQLiteException>(() => connection.ExecuteAsync(
            "INSERT INTO Operations (Id, ProductId, Direction, Quantity, CreatedByUserId, CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, ?, ?, ?)",
            NewId(), productId, "In", quantity, ManagerId, Now, Now));
    }

    [Fact]
    public async Task Operations_InvalidDirection_IsRejected()
    {
        var connection = await Db();
        var productId = await ExistingProductIdAsync();

        await Assert.ThrowsAsync<SQLiteException>(() => connection.ExecuteAsync(
            "INSERT INTO Operations (Id, ProductId, Direction, Quantity, CreatedByUserId, CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, ?, ?, ?)",
            NewId(), productId, "Sideways", 1, ManagerId, Now, Now));
    }

    [Fact]
    public async Task Operations_UnknownProduct_IsRejectedByTheForeignKey()
    {
        var connection = await Db();

        await Assert.ThrowsAsync<SQLiteException>(() => connection.ExecuteAsync(
            "INSERT INTO Operations (Id, ProductId, Direction, Quantity, CreatedByUserId, CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, ?, ?, ?)",
            NewId(), NewId(), "In", 1, ManagerId, Now, Now));
    }

    [Fact]
    public async Task Users_InvalidRole_IsRejected()
    {
        var connection = await Db();

        await Assert.ThrowsAsync<SQLiteException>(() => connection.ExecuteAsync(
            "INSERT INTO Users (Id, FullName, Role, CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, ?)",
            NewId(), "Admin Person", "Admin", Now, Now));
    }

    [Fact]
    public async Task Characteristics_InvalidValueType_IsRejected()
    {
        var connection = await Db();

        await Assert.ThrowsAsync<SQLiteException>(() => connection.ExecuteAsync(
            "INSERT INTO Characteristics (Id, Name, ValueType, CreatedByUserId, CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, ?, ?)",
            NewId(), "Shelf life", "Date", ManagerId, Now, Now));
    }

    [Fact]
    public async Task SyncQueue_InvalidOperation_IsRejected()
    {
        var connection = await Db();

        await Assert.ThrowsAsync<SQLiteException>(() => connection.ExecuteAsync(
            "INSERT INTO SyncQueue (Id, EntityType, EntityId, Operation, PayloadJson, CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, ?, ?, ?)",
            NewId(), "Products", NewId(), "Upsert", "{}", Now, Now));
    }

    [Fact]
    public async Task ForeignKeys_AreSwitchedOnForTheConnection()
    {
        Assert.Equal(1, await (await Db()).ExecuteScalarAsync<int>("PRAGMA foreign_keys"));
    }

    private sealed class PairRow
    {
        public string ProductId { get; set; } = string.Empty;
        public string CharacteristicId { get; set; } = string.Empty;
    }
}
