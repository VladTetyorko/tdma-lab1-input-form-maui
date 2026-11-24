namespace Mobile_apps_L1_Model_view.Services;

public sealed class MauiAppDataPathProvider : IAppDataPathProvider
{
    public string AppDataDirectory => FileSystem.AppDataDirectory;
}
