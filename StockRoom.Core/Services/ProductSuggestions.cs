using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Which products to suggest for typed text. Name and code only (the list search also matches characteristic values).
// Pure and without I/O: a handful of products filter instantly, so there is no debounce
public static class ProductSuggestions
{
    private const int MinimumLetters = 2;

    // A code can be typed from its first digit; a name needs two letters to narrow anything down
    public static bool IsSearchable(string? text)
    {
        var query = text?.Trim() ?? string.Empty;
        return query.Length >= MinimumLetters || (query.Length > 0 && query.All(char.IsAsciiDigit));
    }

    public static IReadOnlyList<ProductSuggestion> Find(
        IEnumerable<ProductSummary> products,
        IReadOnlyList<Characteristic> characteristics,
        string? text,
        int max)
    {
        if (!IsSearchable(text))
        {
            return [];
        }

        var query = text!.Trim();
        return
        [
            .. products
                .Select(product => (Product: product, Rank: Rank(product, query)))
                .Where(match => match.Rank is not null)
                .OrderBy(match => match.Rank)
                .ThenBy(match => match.Product.Name, StringComparer.OrdinalIgnoreCase)
                .Take(max)
                .Select(match => Describe(match.Product, characteristics, query))
        ];
    }

    // Lower is better: exact code, code prefix, name prefix, word prefix, anywhere. Null = no match
    private static int? Rank(ProductSummary product, string query)
    {
        if (string.Equals(product.Barcode, query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (TextMatch.StartsWith(product.Barcode, query))
        {
            return 1;
        }

        if (TextMatch.StartsWith(product.Name, query))
        {
            return 2;
        }

        if (product.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(word => TextMatch.StartsWith(word, query)))
        {
            return 3;
        }

        return TextMatch.Contains(product.Name, query) || TextMatch.Contains(product.Barcode, query) ? 4 : null;
    }

    private static ProductSuggestion Describe(
        ProductSummary product, IReadOnlyList<Characteristic> characteristics, string query)
    {
        var details = characteristics
            .Select(characteristic => product.Values.GetValueOrDefault(characteristic.Id))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Take(2);
        return new ProductSuggestion(
            product,
            string.Join(" · ", details),
            product.Quantity > 0 ? $"{product.Quantity} on hand" : "Out of stock",
            product.Barcode == query);
    }
}
