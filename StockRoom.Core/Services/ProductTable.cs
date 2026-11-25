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

    // A phone: the name keeps as much room as possible
    private const double NarrowQuantityWidth = 56;
    private const double NarrowValueWidth = 88;

    public static TableColumn NameColumn(SortOption sorted, bool descending) =>
        Mark(new TableColumn("Name", SortOption.ByName, 0), sorted, descending);

    // Wide: barcode, quantity, every characteristic. Narrow: quantity and one chosen characteristic (the first by default)
    public static IReadOnlyList<TableColumn> Columns(
        IReadOnlyList<SortOption> options, SortOption sorted, bool descending, bool narrow, SortOption? extra)
    {
        var characteristics = options.Where(option => option.Kind == SortKind.Characteristic).ToList();
        List<TableColumn> columns = [];
        if (!narrow)
        {
            columns.Add(new TableColumn("Barcode", null, BarcodeWidth));
        }

        columns.Add(new TableColumn("Qty", SortOption.ByQuantity, narrow ? NarrowQuantityWidth : QuantityWidth,
            IsNumeric: true));
        var shown = narrow ? Chosen(characteristics, extra) : characteristics;
        columns.AddRange(shown.Select(option => new TableColumn(
            option.Title, option, narrow ? NarrowValueWidth : ValueWidth, option.ValueType == Models.CharacteristicValueType.Number)));
        return [.. columns.Select(column => Mark(column, sorted, descending))];
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

    private static IEnumerable<SortOption> Chosen(List<SortOption> characteristics, SortOption? extra) =>
        extra is not null && characteristics.Contains(extra) ? [extra] : characteristics.Take(1);

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
