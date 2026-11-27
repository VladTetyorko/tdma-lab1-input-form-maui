namespace Mobile_apps_L1_Model_view.Data.Migrations;

public interface IMigrationSource
{
    // All known migrations, ordered by version
    IReadOnlyList<Migration> GetMigrations();
}
