using System.Text.Json;

namespace Mobile_apps_L1_Model_view.Services;

// Finds the Sentry DSN: the environment variable wins, then appsettings.local.json ({ "Sentry": { "Dsn": "..." } }).
// Neither is committed. No DSN = no crash reporting (NullCrashReporter).
public static class SentryDsn
{
    public const string EnvironmentVariable = "STOCKROOM_SENTRY_DSN";
    public const string SettingsFileName = "appsettings.local.json";

    // Null when nothing usable is configured; a malformed file is treated as "not configured"
    public static string? Resolve(string? environmentValue, string? settingsJson)
    {
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue.Trim();
        }

        return FromJson(settingsJson);
    }

    public static string? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("Sentry", out var sentry)
                && sentry.TryGetProperty("Dsn", out var dsn)
                && dsn.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(dsn.GetString()))
            {
                return dsn.GetString()!.Trim();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
