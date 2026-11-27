using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

// Open Food Facts (ODbL) lookup over HTTP. The HttpClient comes from DI; CreateClient sets the shared
// settings so the registration and the tests build it the same way. Network problems become Offline,
// bad answers become Failed: the app never crashes because of this service.
public sealed class OpenFoodFactsLookup(HttpClient http, ILogger<OpenFoodFactsLookup> logger) : IProductLookup
{
    public const string BaseUrl = "https://world.openfoodfacts.org/";
    public const string UserAgent = "StockRoom/1.0 (teaching project)";
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public string Attribution => "Data: Open Food Facts (ODbL)";

    public static HttpClient CreateClient(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = Timeout
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return client;
    }

    public async Task<LookupResult> FindAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var url = $"api/v2/product/{Uri.EscapeDataString(barcode)}?fields=product_name,brands,quantity,categories_tags";
        try
        {
            using var response = await http.GetAsync(url, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return LookupResult.NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Open Food Facts answered {StatusCode} for {Barcode}", (int)response.StatusCode, barcode);
                return LookupResult.Failed();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return Parse(json, barcode);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Open Food Facts is not reachable for {Barcode}", barcode);
            return LookupResult.Offline();
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient reports its own timeout as a cancellation
            logger.LogWarning(ex, "Open Food Facts timed out for {Barcode}", barcode);
            return LookupResult.Offline();
        }
    }

    private LookupResult Parse(string json, string barcode)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
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
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Open Food Facts sent unreadable data for {Barcode}", barcode);
            return LookupResult.Failed();
        }
    }

    private static string Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!.Trim()
            : string.Empty;

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
