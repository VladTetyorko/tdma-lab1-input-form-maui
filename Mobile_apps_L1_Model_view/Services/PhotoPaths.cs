using System.Globalization;

namespace Mobile_apps_L1_Model_view.Services;

// Where product photos live: files in "<app data>/photos", the database stores the path relative to the app data
// folder with forward slashes ("photos/4006381333931_20260115_093000.jpg"), so it stays valid after a restore or an
// app update that moves the data folder
public static class PhotoPaths
{
    public const string Folder = "photos";

    public static string Build(string barcode, DateTime utc) =>
        $"{Folder}/{barcode}_{utc.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)}.jpg";

    // Relative, inside the photos folder, no ".." and no drive or root: nothing that could point outside the app data
    public static bool IsValidRelative(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && path.StartsWith(Folder + "/", StringComparison.Ordinal)
        && !path.Contains("..", StringComparison.Ordinal)
        && !path.Contains('\\')
        && !path.Contains(':');
}
