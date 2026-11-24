namespace Mobile_apps_L1_Model_view.Services;

// Where the real sync server is and which public keys its certificate may have, from appsettings.local.json:
// { "Sync": { "BaseUrl": "https://sync.example.com/", "Pins": ["base64 SHA-256 of the SubjectPublicKeyInfo", ...] } }
// Without a usable https BaseUrl the app keeps the local stub server
public sealed record SyncSettings(Uri? BaseUrl, IReadOnlyList<string> Pins)
{
    public bool IsConfigured => BaseUrl is not null;

    public static SyncSettings Resolve(string? settingsJson)
    {
        var url = LocalSettings.GetString(settingsJson, "Sync", "BaseUrl");
        // Pinning protects a TLS connection, so a plain http address is not accepted at all
        var baseUrl = Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri : null;
        return new SyncSettings(baseUrl, LocalSettings.GetStrings(settingsJson, "Sync", "Pins"));
    }
}
