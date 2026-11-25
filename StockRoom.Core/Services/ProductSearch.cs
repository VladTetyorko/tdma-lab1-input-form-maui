using Mobile_apps_L1_Model_view.Services.Search;

namespace Mobile_apps_L1_Model_view.Services;

// The filter of the product table: ProductMatcher over name, barcode and characteristic values. Empty or blank text
// matches everything. The order is kept, so the sort of the table stays what the user chose
public static class ProductSearch
{
    public static IReadOnlyList<MatchResult> Filter(IEnumerable<ProductSummary> products, string? text)
    {
        var query = SearchQuery.Parse(text);
        return query.IsEmpty
            ? [.. products.Select(product => new MatchResult(product, 0, false, [], []))]
            : [.. products.Select(product => ProductMatcher.Match(product, query, includeValues: true)).OfType<MatchResult>()];
    }
}
