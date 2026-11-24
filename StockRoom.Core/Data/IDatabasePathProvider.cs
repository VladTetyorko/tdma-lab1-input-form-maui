namespace Mobile_apps_L1_Model_view.Data;

// Where the database lives. A platform abstraction so tests can use a temporary file
public interface IDatabasePathProvider
{
    // The SQLite file (stockroom.db3)
    string DatabasePath { get; }

    // A folder the share sheet may read from (copies of the database are made here)
    string ExportDirectory { get; }
}
