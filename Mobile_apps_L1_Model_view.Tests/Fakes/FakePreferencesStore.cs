using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Dictionary-backed preferences; share one instance between two AppStateService objects to
// simulate "the app was killed and started again"
public sealed class FakePreferencesStore : IPreferencesStore
{
    public Dictionary<string, string> Values { get; } = [];

    public string? Get(string key) => Values.GetValueOrDefault(key);

    public void Set(string key, string value) => Values[key] = value;

    public void Remove(string key) => Values.Remove(key);
}
