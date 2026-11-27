using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services.Lookup;

// GET /api/v2/product/{barcode} of one database of the Open Food Facts family
public sealed class OpenFactsSource(HttpClient http, OpenFactsSite site, ILogger<OpenFactsSource> logger)
    : HttpLookupSource(http, logger)
{
    public override string Name => site.Name;

    protected override string Attribution => site.Attribution;

    protected override Uri Address(string barcode) => new(
        $"https://world.{site.Host}/api/v2/product/{Uri.EscapeDataString(barcode)}?fields=product_name,brands,quantity,categories_tags");

    protected override LookupResult Read(JsonElement root, string barcode)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.Number
            || status.GetInt32() != 1
            || !root.TryGetProperty("product", out var product) || product.ValueKind != JsonValueKind.Object)
        {
            return LookupResult.NotFound();
        }

        var name = Text(product, "product_name");
        return name.Length == 0
            ? LookupResult.NotFound()
            : LookupResult.Found(name, FirstBrand(Text(product, "brands")), Text(product, "quantity"), Category(product));
    }

    private static string FirstBrand(string brands) =>
        brands.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

    // The last English tag is the most specific one: "en:chocolate-spreads" -> "Chocolate spreads"
    private static string Category(JsonElement product)
    {
        if (!product.TryGetProperty("categories_tags", out var tags) || tags.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var tag = tags.EnumerateArray()
            .Where(t => t.ValueKind == JsonValueKind.String)
            .Select(t => t.GetString()!)
            .LastOrDefault(t => t.StartsWith("en:", StringComparison.Ordinal));
        if (tag is null)
        {
            return string.Empty;
        }

        var words = tag["en:".Length..].Replace('-', ' ').Trim();
        return words.Length == 0 ? string.Empty : char.ToUpperInvariant(words[0]) + words[1..];
    }
}
