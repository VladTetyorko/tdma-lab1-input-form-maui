namespace Mobile_apps_L1_Model_view.Services.Lookup;

// What the new-product wizard is opened with for an unknown code: the code, and what a database said about it
public static class LookupParameters
{
    public static Dictionary<string, object> ForNewProduct(string barcode, LookupResult? found)
    {
        var parameters = new Dictionary<string, object> { [NavigationKeys.Barcode] = barcode };
        if (found is { IsFound: true })
        {
            parameters[NavigationKeys.ProductName] = found.Name;
            parameters[NavigationKeys.DataSource] = found.Attribution;
        }

        return parameters;
    }
}
