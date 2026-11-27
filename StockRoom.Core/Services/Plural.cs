namespace Mobile_apps_L1_Model_view.Services;

// "1 unit", "12 units": every text with a number goes through here
public static class Plural
{
    public static string Units(int count) => Of(count, "unit");

    public static string Products(int count) => Of(count, "product");

    public static string Lines(int count) => Of(count, "line");

    private static string Of(int count, string word) => count == 1 ? $"1 {word}" : $"{count} {word}s";
}
