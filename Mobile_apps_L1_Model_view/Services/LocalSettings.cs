using System.Text.Json;

namespace Mobile_apps_L1_Model_view.Services;

// Reads appsettings.local.json, the untracked file with this machine's settings (Sentry DSN, sync server).
// A missing, empty or malformed file simply means "not configured": nothing here throws
public static class LocalSettings
{
    public const string FileName = "appsettings.local.json";

    // { "Section": { "Key": "text" } } -> "text"; null when absent, not text or blank
    public static string? GetString(string? json, string section, string key)
    {
        return TryGetProperty(json, section, key, out var value) && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString())
                ? value.GetString()!.Trim()
                : null;
    }

    // { "Section": { "Key": ["a", "b"] } } -> ["a", "b"] (blank and non-text entries are skipped)
    public static IReadOnlyList<string> GetStrings(string? json, string section, string key)
    {
        if (!TryGetProperty(json, section, key, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(e.GetString()))
            .Select(e => e.GetString()!.Trim())
            .ToList();
    }

    private static bool TryGetProperty(string? json, string section, string key, out JsonElement value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(section, out var sectionElement)
                && sectionElement.ValueKind == JsonValueKind.Object
                && sectionElement.TryGetProperty(key, out var found))
            {
                value = found.Clone();
                return true;
            }
        }
        catch (JsonException)
        {
        }

        return false;
    }
}
