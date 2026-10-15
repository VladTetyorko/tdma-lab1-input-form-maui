namespace Mobile_apps_L1_Model_view.Services;

// Keys of the parameter dictionaries passed through INavigationService
public static class NavigationKeys
{
    // ProductSummary: the product a page was opened for (list -> detail, detail -> operation)
    public const string SelectedProduct = "SelectedProduct";

    // ProductSummary: a product that was added or changed, sent back to the list
    public const string UpdatedProduct = "UpdatedProduct";

    // bool: the characteristic catalogue changed, the list must rebuild its sort options
    public const string CharacteristicsChanged = "CharacteristicsChanged";

    // string: a barcode (scanner -> add-product form: prefill the barcode that was not found)
    public const string Barcode = "Barcode";

    // bool: products or stock changed on pages the list is not told about (scanner flow), reload it
    public const string ProductsChanged = "ProductsChanged";
}
