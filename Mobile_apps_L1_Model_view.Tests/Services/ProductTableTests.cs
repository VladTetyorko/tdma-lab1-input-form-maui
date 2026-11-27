using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ProductTableTests
{
    private static readonly SortOption Brand = new("Brand", SortKind.Characteristic, Guid.NewGuid(), CharacteristicValueType.Text);
    private static readonly SortOption Weight = new("Weight, g", SortKind.Characteristic, Guid.NewGuid(), CharacteristicValueType.Number);
    private static readonly IReadOnlyList<SortOption> Options = [SortOption.ByName, SortOption.ByQuantity, Brand, Weight];

    [Fact]
    public void Columns_Wide_ShowBarcodeQuantityAndEveryCharacteristic()
    {
        var columns = ProductTable.Columns(Options, SortOption.ByName, false);

        Assert.Equal(["Barcode", "Qty", "Brand", "Weight, g"], columns.Select(column => column.Title));
        Assert.Null(columns[0].Sort);
    }

    [Fact]
    public void Columns_MarkOnlyTheSortedOne()
    {
        var columns = ProductTable.Columns(Options, Weight, true);

        var sorted = Assert.Single(columns, column => column.IsSorted);
        Assert.Equal("▼ Weight, g", sorted.Header);
        Assert.True(sorted.IsDescending);
        Assert.Contains("sorted descending", sorted.Description);
        Assert.False(ProductTable.NameColumn(Weight, true).IsSorted);
        Assert.Equal("Name ▲", ProductTable.NameColumn(SortOption.ByName, false).Header);
    }

    [Fact]
    public void Columns_HaveFixedWidths_AndNumbersAreMarkedAsNumeric()
    {
        var columns = ProductTable.Columns(Options, SortOption.ByName, false);

        Assert.Equal([(130d, false), (96d, true), (110d, false), (110d, true)],
            columns.Select(column => (column.Width, column.IsNumeric)));
    }

    [Fact]
    public void MinimumWidth_IsPaddingNameColumnsAndSpacing()
    {
        var columns = ProductTable.Columns(Options, SortOption.ByName, false);

        // 24 padding + 180 name + (130 + 96 + 110 + 110) + 4 gaps of 8
        Assert.Equal(24 + 180 + 446 + 32, ProductTable.MinimumWidth(columns, withActions: false));
        // + the actions column (104: two 48 icon buttons and a gap on a phone) and one more gap
        Assert.Equal(24 + 180 + 446 + 104 + 40, ProductTable.MinimumWidth(columns, withActions: true));
    }

    [Fact]
    public void Toggle_SameColumn_FlipsTheDirection()
    {
        Assert.Equal((SortOption.ByName, true), ProductTable.Toggle(SortOption.ByName, false, SortOption.ByName));
        Assert.Equal((SortOption.ByName, false), ProductTable.Toggle(SortOption.ByName, true, SortOption.ByName));
    }

    [Fact]
    public void Toggle_AnotherColumn_StartsAscending()
    {
        Assert.Equal((Brand, false), ProductTable.Toggle(SortOption.ByName, true, Brand));
    }

    [Fact]
    public void Row_FillsCellsInColumnOrderAndMarksMissingValues()
    {
        var product = new ProductSummary(Guid.NewGuid(), "Nutella", "3017620422003", 0)
        {
            Values = new Dictionary<Guid, string> { [Brand.CharacteristicId] = "Ferrero" }
        };
        var columns = ProductTable.Columns(Options, SortOption.ByName, false);

        var row = ProductTable.Row(product, columns);

        Assert.Equal(["3017620422003", "0", "Ferrero", ProductTable.Missing], row.Cells.Select(cell => cell.Text));
        Assert.True(row.IsOutOfStock);
        Assert.True(row.Cells[1].IsAlert);
        Assert.False(row.Cells[2].IsAlert);
        Assert.Equal(["Header_Barcode", "SortHeader_Quantity"], columns.Take(2).Select(column => column.AutomationId));
        Assert.True(row.CanDelete);
    }
}
