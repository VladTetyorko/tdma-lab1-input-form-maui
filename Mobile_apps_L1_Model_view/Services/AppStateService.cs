using System.Text.Json;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class AppStateService(IPreferencesStore store) : IAppStateService
{
    // Written by versions before Lab 6, which saved the signed-in user id here; removed on the next save
    private const string LegacyUserKey = "state.user";
    private const string RouteKey = "state.route";
    private const string DraftsKey = "state.drafts";

    // Draft type name -> JSON
    private Dictionary<string, string> _drafts = [];
    private string? _resumeRoute;

    public string? LastRoute { get; set; }

    public T? GetDraft<T>() where T : class
    {
        if (!_drafts.TryGetValue(KeyOf<T>(), out var json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            // A draft written by an older version of the app: drop it instead of crashing
            _drafts.Remove(KeyOf<T>());
            return null;
        }
    }

    public void SetDraft<T>(T draft) where T : class
    {
        ArgumentNullException.ThrowIfNull(draft);
        _drafts[KeyOf<T>()] = JsonSerializer.Serialize(draft);
    }

    public void ClearDraft<T>() where T : class => _drafts.Remove(KeyOf<T>());

    public void Save()
    {
        store.Remove(LegacyUserKey);
        Write(RouteKey, LastRoute);
        Write(DraftsKey, _drafts.Count == 0 ? null : JsonSerializer.Serialize(_drafts));
    }

    public void Restore()
    {
        LastRoute = store.Get(RouteKey);
        _resumeRoute = LastRoute;
        _drafts = ReadDrafts();
    }

    public string? TakeResumeRoute()
    {
        var route = _resumeRoute;
        _resumeRoute = null;
        return route;
    }

    public void Clear()
    {
        LastRoute = null;
        _resumeRoute = null;
        _drafts.Clear();
        Save();
    }

    private Dictionary<string, string> ReadDrafts()
    {
        var json = store.Get(DraftsKey);
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private void Write(string key, string? value)
    {
        if (value is null)
        {
            store.Remove(key);
        }
        else
        {
            store.Set(key, value);
        }
    }

    private static string KeyOf<T>() => typeof(T).Name;
}
