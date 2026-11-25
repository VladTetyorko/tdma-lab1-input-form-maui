namespace Mobile_apps_L1_Model_view.Services;

// One product in the table: the name column is drawn by the page, Cells hold the other columns in header order
public sealed record ProductTableRow(ProductSummary Product, IReadOnlyList<ProductTableCell> Cells)
{
    public string Name => Product.Name;

    // What the search matched in the name; IsSimilar = the name is only similar to what was typed
    public IReadOnlyList<Search.TextRange> NameRanges { get; init; } = [];

    public bool IsSimilar { get; init; }

    public string Barcode => Product.Barcode;

    public bool CanDelete => Product.CanDelete;
}
