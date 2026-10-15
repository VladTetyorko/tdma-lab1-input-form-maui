using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Counts the shares; Failure makes ShareAsync throw
public sealed class FakeDatabaseExporter : IDatabaseExporter
{
    public int Shares { get; private set; }

    public Exception? Failure { get; set; }

    public Task<string> ShareAsync()
    {
        Shares++;
        return Failure is null ? Task.FromResult("copy.db3") : Task.FromException<string>(Failure);
    }
}
