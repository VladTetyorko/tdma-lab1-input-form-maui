using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Data.Migrations;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using SQLite;

namespace Mobile_apps_L1_Model_view.Tests.Data;

// The migration runner against a temporary SQLite file: versions, order, rerun and failure handling
public class MigrationRunnerTests : IDisposable
{
    private static readonly string[] StockRoomTables =
        ["Users", "Products", "Characteristics", "ProductCharacteristics", "Operations", "SyncQueue"];

    private readonly TempDatabasePath _paths = new();
    private readonly List<SQLiteAsyncConnection> _connections = [];

    public void Dispose()
    {
        foreach (var connection in _connections)
        {
            connection.CloseAsync().GetAwaiter().GetResult();
        }

        _paths.Dispose();
    }

    private SQLiteAsyncConnection Open()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_paths.DatabasePath)!);
        var connection = new SQLiteAsyncConnection(
            _paths.DatabasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
        _connections.Add(connection);
        return connection;
    }

    private static MigrationRunner StockRoomRunner() => new(new EmbeddedMigrationSource());

    [Fact]
    public async Task FreshDatabase_IsMigratedToTheLatestVersion_AndHasEveryTable()
    {
        var connection = Open();

        var version = await StockRoomRunner().MigrateAsync(connection);

        Assert.Equal(2, version);
        Assert.Equal(2, await connection.ExecuteScalarAsync<int>("PRAGMA user_version"));
        var tables = await connection.QueryScalarsAsync<string>(
            "SELECT name FROM sqlite_master WHERE type = 'table'");
        foreach (var table in StockRoomTables)
        {
            Assert.Contains(table, tables);
        }
    }

    [Fact]
    public async Task RunningAgain_IsANoOp_AndDoesNotSeedTwice()
    {
        var connection = Open();
        var runner = StockRoomRunner();
        await runner.MigrateAsync(connection);
        var usersAfterFirstRun = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users");

        var version = await runner.MigrateAsync(connection);

        Assert.Equal(2, version);
        Assert.Equal(3, usersAfterFirstRun);
        Assert.Equal(usersAfterFirstRun, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users"));
    }

    [Fact]
    public async Task DatabaseNewerThanTheApp_IsRefused_AndNotTouched()
    {
        var connection = Open();
        await StockRoomRunner().MigrateAsync(connection);
        await connection.ExecuteAsync("PRAGMA user_version = 99");
        var runner = new MigrationRunner(new FakeMigrationSource(new Migration(1, "0001_initial", "SELECT 1;")));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.MigrateAsync(connection));

        Assert.Contains("version 99", ex.Message);
        Assert.Equal(99, await connection.ExecuteScalarAsync<int>("PRAGMA user_version"));
    }

    [Fact]
    public async Task FailingMigration_RollsBack_AndKeepsTheOldVersion()
    {
        var connection = Open();
        var runner = new MigrationRunner(new FakeMigrationSource(
            new Migration(1, "0001_first", "CREATE TABLE First (Id INTEGER);"),
            new Migration(2, "0002_broken", "CREATE TABLE Second (Id INTEGER); INSERT INTO MissingTable VALUES (1);")));

        await Assert.ThrowsAsync<SQLiteException>(() => runner.MigrateAsync(connection));

        Assert.Equal(1, await connection.ExecuteScalarAsync<int>("PRAGMA user_version"));
        var tables = await connection.QueryScalarsAsync<string>("SELECT name FROM sqlite_master WHERE type = 'table'");
        Assert.Contains("First", tables);
        Assert.DoesNotContain("Second", tables);
    }

    [Fact]
    public async Task FailingMigration_OnAnExistingDatabase_LeavesItAtItsVersion()
    {
        var connection = Open();
        await new MigrationRunner(new FakeMigrationSource(new Migration(1, "0001", "CREATE TABLE Kept (Id INTEGER);")))
            .MigrateAsync(connection);
        var runner = new MigrationRunner(new FakeMigrationSource(
            new Migration(1, "0001", "CREATE TABLE Kept (Id INTEGER);"),
            new Migration(2, "0002", "ALTER TABLE Kept ADD COLUMN Extra TEXT; SELECT * FROM Nope;")));

        await Assert.ThrowsAsync<SQLiteException>(() => runner.MigrateAsync(connection));

        Assert.Equal(1, await connection.ExecuteScalarAsync<int>("PRAGMA user_version"));
    }

    [Fact]
    public async Task ScriptsAreAppliedInVersionOrder()
    {
        var connection = Open();
        var runner = new MigrationRunner(new FakeMigrationSource(
            new Migration(3, "0003_c", "INSERT INTO Log VALUES ('three');"),
            new Migration(1, "0001_a", "CREATE TABLE Log (Step TEXT); INSERT INTO Log VALUES ('one');"),
            new Migration(2, "0002_b", "INSERT INTO Log VALUES ('two');")));

        var version = await runner.MigrateAsync(connection);

        Assert.Equal(3, version);
        var steps = await connection.QueryScalarsAsync<string>("SELECT Step FROM Log ORDER BY rowid");
        Assert.Equal(["one", "two", "three"], steps);
    }

    [Fact]
    public void EmbeddedSource_ListsTheScriptsInVersionOrder()
    {
        var migrations = new EmbeddedMigrationSource().GetMigrations();

        Assert.Equal([1, 2], migrations.Select(m => m.Version));
        Assert.Equal(["0001_initial", "0002_seed"], migrations.Select(m => m.Name));
        Assert.All(migrations, m => Assert.False(string.IsNullOrWhiteSpace(m.Sql)));
    }

    private sealed class FakeMigrationSource(params Migration[] migrations) : IMigrationSource
    {
        public IReadOnlyList<Migration> GetMigrations() => migrations.OrderBy(m => m.Version).ToList();
    }
}
