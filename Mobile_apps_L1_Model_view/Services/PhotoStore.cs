using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

// The files of product photos: copies a picked or captured image into "<app data>/photos" and hands back the path
// RELATIVE to the app data folder. Plain System.IO, so it is tested without a camera
public sealed class PhotoStore(IAppDataPathProvider appData, IClock clock, ILogger<PhotoStore> logger)
{
    public async Task<string> SaveAsync(Stream source, string barcode)
    {
        var relative = PhotoPaths.Build(barcode, clock.UtcNow);
        var full = Path.Combine(appData.AppDataDirectory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var target = File.Create(full);
        await source.CopyToAsync(target);
        logger.LogDebug("Photo for barcode {Barcode} stored as {Path}", barcode, relative);
        return relative;
    }

    // Full path for display; null for no photo, a path outside the photos folder, or a file that is gone
    public string? ResolvePath(string? relativePath)
    {
        if (!PhotoPaths.IsValidRelative(relativePath))
        {
            return null;
        }

        var full = Path.Combine(appData.AppDataDirectory, relativePath!);
        return File.Exists(full) ? full : null;
    }

    public bool Delete(string? relativePath)
    {
        if (ResolvePath(relativePath) is not { } full)
        {
            return false;
        }

        try
        {
            File.Delete(full);
            logger.LogDebug("Photo file {Path} deleted", relativePath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Photo file {Path} could not be deleted", relativePath);
            return false;
        }
    }
}
