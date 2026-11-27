using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// A temporary app data folder, deleted on Dispose
public sealed class FakeAppDataPath : IAppDataPathProvider, IDisposable
{
    public string AppDataDirectory { get; } = Path.Combine(Path.GetTempPath(), "stockroom-appdata-" + Guid.NewGuid().ToString("N"));

    public FakeAppDataPath() => Directory.CreateDirectory(AppDataDirectory);

    public void Dispose()
    {
        try
        {
            Directory.Delete(AppDataDirectory, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
