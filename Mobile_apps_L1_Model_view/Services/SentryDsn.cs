namespace Mobile_apps_L1_Model_view.Services;

// Finds the Sentry DSN: the environment variable wins, then appsettings.local.json ({ "Sentry": { "Dsn": "..." } }).
// Neither is committed. No DSN = no crash reporting (NullCrashReporter).
public static class SentryDsn
{
    public const string EnvironmentVariable = "STOCKROOM_SENTRY_DSN";
    public const string SettingsFileName = LocalSettings.FileName;

    // Null when nothing usable is configured; a malformed file is treated as "not configured"
    public static string? Resolve(string? environmentValue, string? settingsJson)
    {
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue.Trim();
        }

        return FromJson(settingsJson);
    }

    public static string? FromJson(string? json) => LocalSettings.GetString(json, "Sentry", "Dsn");
}
