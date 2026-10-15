using Mobile_apps_L1_Model_view.Data;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// A database file in its own temporary folder; Dispose deletes the folder (dispose the database first)
public sealed class TempDatabasePath : IDatabasePathProvider, IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "stockroom-tests", Guid.NewGuid().ToString("N"));

    public string DatabasePath => Path.Combine(_folder, "stockroom.db3");

    public string ExportDirectory => Path.Combine(_folder, "export");

    public void Dispose()
    {
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp folder is harmless
        }
    }
}
