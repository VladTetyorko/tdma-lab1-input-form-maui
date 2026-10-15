namespace Mobile_apps_L1_Model_view.ViewModels;

// One row of the product list: a product with its computed quantity on hand
public sealed record ProductListItem(Guid Id, string Name, string Barcode, int Quantity);
