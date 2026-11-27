using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services.Search;

namespace Mobile_apps_L1_Model_view.Services;

// Which products to suggest for typed text: ProductMatcher on name and code (the table also searches characteristic
// values). Pure and without I/O: a handful of products filter instantly, so there is no debounce
public static class ProductSuggestions
{
    private const int MinimumLetters = 2;

    // A code can be typed from its first digit; a name needs two letters to narrow anything down
    public static bool IsSearchable(string? text)
    {
        var query = text?.Trim() ?? string.Empty;
        return query.Length >= MinimumLetters || (query.Length > 0 && query.All(char.IsAsciiDigit));
    }

    // Similar rows only fill the places the exact ones left free
    public static IReadOnlyList<ProductSuggestion> Find(
        IEnumerable<ProductSummary> products,
        IReadOnlyList<Characteristic> characteristics,
        string? text,
        int max,
        bool inStockFirst = false)
    {
        if (!IsSearchable(text))
        {
            return [];
        }

        var query = text!.Trim();
        return
        [
            .. ProductMatcher.Rank(products, query, inStockFirst)
                .OrderBy(match => match.IsFuzzy)
                .Take(max)
                .Select(match => Describe(match, characteristics, query))
        ];
    }

    private static ProductSuggestion Describe(
        MatchResult match, IReadOnlyList<Characteristic> characteristics, string query)
    {
        var product = match.Product;

        // A bare number says nothing: it gets the name of its characteristic
        var details = characteristics
            .Select(characteristic => (characteristic, Value: product.Values.GetValueOrDefault(characteristic.Id)))
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => pair.characteristic.ValueType == CharacteristicValueType.Number
                ? $"{pair.characteristic.Name} {pair.Value}"
                : pair.Value!)
            .Take(2);
        return new ProductSuggestion(
            product,
            string.Join(" · ", details),
            product.Quantity > 0 ? $"{product.Quantity} on hand" : "Out of stock",
            product.Barcode == query)
        {
            IsFuzzy = match.IsFuzzy,
            NameRanges = match.NameRanges,
            CodeRanges = match.CodeRanges
        };
    }
}
