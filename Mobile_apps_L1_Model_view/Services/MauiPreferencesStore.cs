namespace Mobile_apps_L1_Model_view.Services;

// The only class that touches MAUI Preferences
public sealed class MauiPreferencesStore : IPreferencesStore
{
    public string? Get(string key) => Preferences.Default.ContainsKey(key) ? Preferences.Default.Get(key, string.Empty) : null;

    public void Set(string key, string value) => Preferences.Default.Set(key, value);

    public void Remove(string key) => Preferences.Default.Remove(key);
}
