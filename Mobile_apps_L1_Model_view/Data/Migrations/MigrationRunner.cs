using SQLite;

namespace Mobile_apps_L1_Model_view.Data.Migrations;

// Brings the database up to the latest schema. The version lives in PRAGMA user_version (a number in the
// database file header). Each pending script runs in its own transaction together with the version bump,
// so a failed script leaves the previous version intact.
public sealed class MigrationRunner(IMigrationSource source)
{
    // Returns the schema version after the run
    public async Task<int> MigrateAsync(SQLiteAsyncConnection connection)
    {
        var migrations = source.GetMigrations();
        var current = await connection.ExecuteScalarAsync<int>("PRAGMA user_version");
        var latest = migrations.Count == 0 ? 0 : migrations[^1].Version;
        if (current > latest)
        {
            throw new InvalidOperationException(
                $"The database is at version {current}, but this app only knows version {latest}. Update the app.");
        }

        foreach (var migration in migrations.Where(m => m.Version > current))
        {
            await connection.RunInTransactionAsync(transaction =>
            {
                foreach (var statement in SqlScriptSplitter.Split(migration.Sql))
                {
                    transaction.Execute(statement);
                }

                transaction.Execute($"PRAGMA user_version = {migration.Version}");
            });
            current = migration.Version;
        }

        return current;
    }
}
