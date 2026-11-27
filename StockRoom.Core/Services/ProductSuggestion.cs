using Mobile_apps_L1_Model_view.Services.Search;

namespace Mobile_apps_L1_Model_view.Services;

// One row of the quick search: the name first, then what tells two products apart, the code last
public sealed record ProductSuggestion(
    ProductSummary Product,
    string Details,
    string StockText,
    bool IsExactCode)
{
    public const string SimilarCaption = "Similar name";

    public string Title => Product.Name;

    public string Code => Product.Barcode;

    // "Ferrero · 400 g · 3017620422003", or the code alone
    public string DetailsAndCode => Details.Length == 0 ? Code : $"{Details} · {Code}";

    // Only similar to what was typed: it is never picked without the user
    public bool IsFuzzy { get; init; }

    // Parts of the name and the code that match what was typed
    public IReadOnlyList<TextRange> NameRanges { get; init; } = [];

    public IReadOnlyList<TextRange> CodeRanges { get; init; } = [];

    public bool IsOutOfStock => Product.Quantity <= 0;

    public string SimilarText => IsFuzzy ? SimilarCaption : string.Empty;
}
