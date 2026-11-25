namespace Mobile_apps_L1_Model_view.Services;

// One row of the quick search: the name first, then what tells two products apart, the code last
public sealed record ProductSuggestion(
    ProductSummary Product,
    string Details,
    string StockText,
    bool IsExactCode)
{
    public string Title => Product.Name;

    public string Code => Product.Barcode;

    // "Ferrero · 400 g · 3017620422003", or the code alone
    public string DetailsAndCode => Details.Length == 0 ? Code : $"{Details} · {Code}";
}
