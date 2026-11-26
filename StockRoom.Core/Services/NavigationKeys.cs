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

    // string: a product name (scanner -> add-product form: prefill the name found in an open database)
    public const string ProductName = "ProductName";

    // string: where prefilled data comes from, shown as attribution on the form (licence requirement)
    public const string DataSource = "DataSource";

    // bool: show the product table with the empty products first (Home's out-of-stock list)
    public const string SortByQuantity = "SortByQuantity";

    // bool: products or stock changed on pages the list is not told about (scanner flow), reload it
    public const string ProductsChanged = "ProductsChanged";
}
