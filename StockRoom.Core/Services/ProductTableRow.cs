namespace Mobile_apps_L1_Model_view.Services;

// One product in the table: the name column is drawn by the page, Cells hold the other columns in header order
public sealed record ProductTableRow(ProductSummary Product, IReadOnlyList<ProductTableCell> Cells)
{
    public string Name => Product.Name;

    public string Barcode => Product.Barcode;

    public bool CanDelete => Product.CanDelete;
}
