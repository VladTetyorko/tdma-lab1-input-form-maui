using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// A log folder of its own in the temp directory; Dispose deletes it
public sealed class TempLogPath : ILogPathProvider, IDisposable
{
    public string LogDirectory { get; } = Path.Combine(Path.GetTempPath(), "stockroom-tests", "logs-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try
        {
            Directory.Delete(LogDirectory, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp folder is harmless
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
