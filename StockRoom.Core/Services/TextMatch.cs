namespace Mobile_apps_L1_Model_view.Services;

// How typed text is compared with stored text, shared by the list search and the quick search
public static class TextMatch
{
    public static bool Contains(string? source, string query) =>
        source is not null && source.Contains(query, StringComparison.OrdinalIgnoreCase);

    public static bool StartsWith(string? source, string query) =>
        source is not null && source.StartsWith(query, StringComparison.OrdinalIgnoreCase);
}
