using SQLite;

namespace Mobile_apps_L1_Model_view.Data;

// The one database of the app. The connection is opened, configured and migrated on first use,
// so every repository simply awaits it and nothing has to initialise the database at startup.
public interface IDatabase
{
    // Opens the file, turns foreign keys on, applies pending migrations (once), returns the shared connection
    Task<SQLiteAsyncConnection> GetConnectionAsync();

    // Writes a complete, self-contained copy of the database to targetPath (WAL is checkpointed first)
    Task CopyToAsync(string targetPath);
}
