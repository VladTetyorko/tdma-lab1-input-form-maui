namespace Mobile_apps_L1_Model_view.Services;

// Hands a file to the system share sheet (platform abstraction over MAUI Share)
public interface IFileSharer
{
    Task ShareFileAsync(string title, string filePath);
}
