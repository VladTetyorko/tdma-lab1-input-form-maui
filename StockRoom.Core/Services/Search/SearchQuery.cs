namespace Mobile_apps_L1_Model_view.Services.Search;

// A word of what was typed. Digits are compared with barcodes (never approximately), letters with names
public sealed record SearchToken(string Text)
{
    public bool IsDigits => Text.All(char.IsAsciiDigit);
}

// What was typed, as words that must all match
public sealed record SearchQuery(IReadOnlyList<SearchToken> Tokens)
{
    public bool IsEmpty => Tokens.Count == 0;

    public static SearchQuery Parse(string? text) =>
        new([.. SearchText.From(text).Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => new SearchToken(word))]);
}
