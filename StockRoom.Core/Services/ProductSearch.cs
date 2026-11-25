namespace Mobile_apps_L1_Model_view.Services;

// Pure filtering of product rows by what the user typed, so the rule is testable without a ViewModel.
// A product matches when the trimmed text appears (ignoring case) in its name, its barcode or
// any of its characteristic values. Empty or blank text matches everything. Order is kept.
public static class ProductSearch
{
    public static IReadOnlyList<ProductSummary> Filter(IEnumerable<ProductSummary> products, string? text)
    {
        var query = text?.Trim();
        return string.IsNullOrEmpty(query)
            ? [.. products]
            : [.. products.Where(p => Matches(p, query))];
    }

    private static bool Matches(ProductSummary product, string query) =>
        TextMatch.Contains(product.Name, query)
        || TextMatch.Contains(product.Barcode, query)
        || product.Values.Values.Any(value => TextMatch.Contains(value, query));
}
