namespace Mobile_apps_L1_Model_view.Services;

public interface IDatabaseExporter
{
    // Makes a complete copy of the database and opens the share sheet for it. Returns the copy's path
    Task<string> ShareAsync();
}
