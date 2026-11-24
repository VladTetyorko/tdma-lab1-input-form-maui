namespace Mobile_apps_L1_Model_view.Services;

// Small persistent key/value storage. Keeps services free of the MAUI Preferences static
public interface IPreferencesStore
{
    // Null when the key was never set
    string? Get(string key);

    void Set(string key, string value);

    void Remove(string key);
}
