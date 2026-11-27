using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;

namespace Mobile_apps_L1_Model_view.Services;

// The copy (not the live file) is shared, so the share sheet cannot touch the open database
public sealed class DatabaseExporter(
    IDatabase database,
    IDatabasePathProvider paths,
    IFileSharer sharer,
    ILogger<DatabaseExporter> logger) : IDatabaseExporter
{
    public const string ExportFileName = "stockroom-export.db3";

    public async Task<string> ShareAsync()
    {
        var copyPath = Path.Combine(paths.ExportDirectory, ExportFileName);
        try
        {
            await database.CopyToAsync(copyPath);
            await sharer.ShareFileAsync("StockRoom database", copyPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sharing a copy of the database failed");
            throw;
        }

        logger.LogInformation("Shared a copy of the database");
        return copyPath;
    }
}
