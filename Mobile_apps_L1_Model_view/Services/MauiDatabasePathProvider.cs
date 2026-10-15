using Mobile_apps_L1_Model_view.Data;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class MauiDatabasePathProvider : IDatabasePathProvider
{
    public string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, "stockroom.db3");

    public string ExportDirectory => FileSystem.CacheDirectory;
}
