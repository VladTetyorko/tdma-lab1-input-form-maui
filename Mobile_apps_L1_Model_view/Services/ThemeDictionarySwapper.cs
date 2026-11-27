namespace Mobile_apps_L1_Model_view.Services;

// Puts exactly one theme dictionary into a merged-dictionaries collection. Kept apart from MauiThemeApplier so it can be tested
// on a plain collection, without a running Application
public static class ThemeDictionarySwapper
{
    public static void Swap(ICollection<ResourceDictionary> merged, ResourceDictionary theme)
    {
        foreach (var old in merged.Where(dictionary => dictionary is IThemeDictionary).ToList())
        {
            merged.Remove(old);
        }

        merged.Add(theme);
    }
}
