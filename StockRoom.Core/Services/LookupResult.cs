namespace Mobile_apps_L1_Model_view.Services;

// What an open database knows about a barcode. Only Found carries product data
public sealed record LookupResult(
    LookupStatus Status,
    string Name = "",
    string Brand = "",
    string QuantityText = "",
    string Category = "")
{
    // Books only
    public string Authors { get; init; } = string.Empty;

    public string Publisher { get; init; } = string.Empty;

    // The database that answered, and the text that must be shown wherever its data is displayed
    public string Source { get; init; } = string.Empty;

    public string Attribution { get; init; } = string.Empty;

    public bool IsFound => Status == LookupStatus.Found;

    // What tells the item apart besides its name: "Roald Dahl · Puffin", "Ferrero · 400 g"
    public string Details => string.Join(" · ",
        new[] { Authors, Publisher, Brand, QuantityText }.Where(text => text.Length > 0));

    public static LookupResult Found(string name, string brand, string quantityText, string category) =>
        new(LookupStatus.Found, name, brand, quantityText, category);

    public static LookupResult NotFound() => new(LookupStatus.NotFound);

    public static LookupResult Offline() => new(LookupStatus.Offline);

    public static LookupResult Failed() => new(LookupStatus.Failed);
}
