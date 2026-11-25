namespace Mobile_apps_L1_Model_view.Services.Search;

// A product that matched, how well, and which parts of its name and barcode to mark.
// IsFuzzy: at least one word was only similar, so the product must never be picked without the user
public sealed record MatchResult(
    ProductSummary Product,
    double Score,
    bool IsFuzzy,
    IReadOnlyList<TextRange> NameRanges,
    IReadOnlyList<TextRange> CodeRanges);
