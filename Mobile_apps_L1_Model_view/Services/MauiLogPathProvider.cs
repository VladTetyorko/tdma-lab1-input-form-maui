namespace Mobile_apps_L1_Model_view.Services;

public sealed class MauiLogPathProvider : ILogPathProvider
{
    public string LogDirectory => Path.Combine(FileSystem.AppDataDirectory, "logs");
}
