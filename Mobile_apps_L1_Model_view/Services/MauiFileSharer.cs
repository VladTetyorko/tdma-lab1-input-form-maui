namespace Mobile_apps_L1_Model_view.Services;

public sealed class MauiFileSharer : IFileSharer
{
    public Task ShareFileAsync(string title, string filePath) =>
        Share.Default.RequestAsync(new ShareFileRequest(title, new ShareFile(filePath)));
}
