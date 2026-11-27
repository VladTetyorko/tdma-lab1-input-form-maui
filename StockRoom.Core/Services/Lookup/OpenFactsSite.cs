namespace Mobile_apps_L1_Model_view.Services.Lookup;

// The databases of the Open Food Facts family: the same API on another host (all ODbL)
public sealed record OpenFactsSite(string Name, string Host)
{
    public static readonly OpenFactsSite Food = new("Open Food Facts", "openfoodfacts.org");
    public static readonly OpenFactsSite Beauty = new("Open Beauty Facts", "openbeautyfacts.org");
    public static readonly OpenFactsSite PetFood = new("Open Pet Food Facts", "openpetfoodfacts.org");
    public static readonly OpenFactsSite Products = new("Open Products Facts", "openproductsfacts.org");

    // The order they are asked in
    public static IReadOnlyList<OpenFactsSite> All => [Food, Beauty, PetFood, Products];

    public string Attribution => $"Data: {Name} (ODbL), {Host}";
}
