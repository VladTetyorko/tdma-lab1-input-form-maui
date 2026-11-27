namespace Mobile_apps_L1_Model_view.Services;

// The folder the app keeps its own files in (platform abstraction over FileSystem.AppDataDirectory).
// Photos are stored below it; the database stores only the path relative to it
public interface IAppDataPathProvider
{
    string AppDataDirectory { get; }
}
