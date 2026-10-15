using Mobile_apps_L1_Model_view.Data.Migrations;
using SQLite;

namespace Mobile_apps_L1_Model_view.Data;

public sealed class SqliteDatabase(IDatabasePathProvider paths, MigrationRunner migrations) : IDatabase, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SQLiteAsyncConnection? _connection;

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _gate.WaitAsync();
        try
        {
            return _connection ??= await OpenAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task CopyToAsync(string targetPath)
    {
        var connection = await GetConnectionAsync();

        // Moves everything from the -wal file into the main file, so copying the one file is enough
        await connection.ExecuteScalarAsync<int>("PRAGMA wal_checkpoint(TRUNCATE)");

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        await using var source = new FileStream(
            paths.DatabasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        await using var target = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
        await source.CopyToAsync(target);
    }

    // Closes the file (tests delete their temporary database afterwards)
    public void Dispose()
    {
        _connection?.CloseAsync().GetAwaiter().GetResult();
        _gate.Dispose();
    }

    private async Task<SQLiteAsyncConnection> OpenAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(paths.DatabasePath)!);
        var connection = new SQLiteAsyncConnection(
            paths.DatabasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
        try
        {
            // SQLite ignores foreign keys unless this is switched on for each connection
            await connection.ExecuteAsync("PRAGMA foreign_keys = ON");
            await connection.EnableWriteAheadLoggingAsync();
            await migrations.MigrateAsync(connection);
            return connection;
        }
        catch
        {
            await connection.CloseAsync();
            throw;
        }
    }
}
