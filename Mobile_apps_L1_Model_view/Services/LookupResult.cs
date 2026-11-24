namespace Mobile_apps_L1_Model_view.Services;

// What an external product database knows about a barcode. Only Found carries product data
public sealed record LookupResult(
    LookupStatus Status,
    string Name = "",
    string Brand = "",
    string QuantityText = "",
    string Category = "")
{
    public bool IsFound => Status == LookupStatus.Found;

    public static LookupResult Found(string name, string brand, string quantityText, string category) =>
        new(LookupStatus.Found, name, brand, quantityText, category);

    public static LookupResult NotFound() => new(LookupStatus.NotFound);

    public static LookupResult Offline() => new(LookupStatus.Offline);

    public static LookupResult Failed() => new(LookupStatus.Failed);
}
