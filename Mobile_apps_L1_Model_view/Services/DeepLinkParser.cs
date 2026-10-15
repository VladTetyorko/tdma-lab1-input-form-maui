namespace Mobile_apps_L1_Model_view.Services;

// Pure parsing of the links other apps use to open StockRoom: stockroom://product/{barcode}
public static class DeepLinkParser
{
    public const string Scheme = "stockroom";
    public const string ProductHost = "product";

    public static string ForProduct(string barcode) => $"{Scheme}://{ProductHost}/{Uri.EscapeDataString(barcode)}";

    // Null when the text is not a product link: wrong scheme or host, no barcode, or extra path segments.
    // Scheme and host ignore case; query and fragment are ignored
    public static ProductDeepLink? Parse(string? link)
    {
        if (!Uri.TryCreate(link?.Trim(), UriKind.Absolute, out var uri)
            || !uri.Scheme.Equals(Scheme, StringComparison.OrdinalIgnoreCase)
            || !uri.Host.Equals(ProductHost, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 1)
        {
            return null;
        }

        var barcode = Uri.UnescapeDataString(segments[0]).Trim();
        return barcode.Length == 0 ? null : new ProductDeepLink(barcode);
    }
}
