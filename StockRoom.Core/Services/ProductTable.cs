using System.Globalization;

namespace Mobile_apps_L1_Model_view.Services;

// The product table as data: which columns exist, which one is sorted and what each cell says. Pure, so the rules
// are tested without a page. The first column (name) is returned apart from the others: it stretches
public static class ProductTable
{
    public const string Missing = "—";

    private const double BarcodeWidth = 130;
    private const double QuantityWidth = 96;
    private const double ValueWidth = 110;

    // The widths of the page around the columns (the row padding, the gap between columns, the edit/delete column)
    private const double NameMinimumWidth = 180;
    private const double RowPadding = 12;
    private const double ColumnSpacing = 8;
    public const double ActionsWidth = 104;

    public static TableColumn NameColumn(SortOption sorted, bool descending) =>
        Mark(new TableColumn("Name", SortOption.ByName, 0), sorted, descending);

    // Always the same columns: barcode, quantity, every characteristic. The table scrolls sideways when they do not fit
    public static IReadOnlyList<TableColumn> Columns(IReadOnlyList<SortOption> options, SortOption sorted, bool descending)
    {
        List<TableColumn> columns =
        [
            new TableColumn("Barcode", null, BarcodeWidth),
            new TableColumn("Qty", SortOption.ByQuantity, QuantityWidth, IsNumeric: true)
        ];
        columns.AddRange(options.Where(option => option.Kind == SortKind.Characteristic).Select(option =>
            new TableColumn(option.Title, option, ValueWidth, option.ValueType == Models.CharacteristicValueType.Number)));
        return [.. columns.Select(column => Mark(column, sorted, descending))];
    }

    // The least width at which the table keeps every column: below it the table scrolls sideways
    public static double MinimumWidth(IReadOnlyList<TableColumn> columns, bool withActions)
    {
        var cells = columns.Count + 1 + (withActions ? 1 : 0);
        return 2 * RowPadding + NameMinimumWidth + columns.Sum(column => column.Width)
               + (withActions ? ActionsWidth : 0) + (cells - 1) * ColumnSpacing;
    }

    // The same column flips its direction; another column starts ascending
    public static (SortOption Sort, bool Descending) Toggle(SortOption current, bool descending, SortOption clicked) =>
        clicked == current ? (current, !descending) : (clicked, false);

    public static ProductTableRow Row(ProductSummary product, IReadOnlyList<TableColumn> columns) =>
        new(product, [.. columns.Select(column => Cell(product, column))]);

    // Quantity stays a number: "out of stock" is said by a chip under the name
    private static ProductTableCell Cell(ProductSummary product, TableColumn column) =>
        new(Text(product, column), column.Width, column.IsNumeric)
        {
            IsAlert = column.Sort == SortOption.ByQuantity && product.Quantity <= 0
        };

    private static string Text(ProductSummary product, TableColumn column) => column.Sort switch
    {
        null => product.Barcode,
        { Kind: SortKind.Quantity } => product.Quantity.ToString(CultureInfo.CurrentCulture),
        { Kind: SortKind.Characteristic } option =>
            product.Values.TryGetValue(option.CharacteristicId, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : Missing,
        _ => product.Name
    };

    private static TableColumn Mark(TableColumn column, SortOption sorted, bool descending) =>
        column.Sort is not null && column.Sort == sorted
            ? column with { IsSorted = true, IsDescending = descending }
            : column;
}
