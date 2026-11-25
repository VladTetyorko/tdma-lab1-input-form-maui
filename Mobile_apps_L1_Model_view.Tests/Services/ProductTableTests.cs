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
        var columns = ProductTable.Columns(Options, SortOption.ByName, false, narrow: false, extra: null);

        Assert.Equal(["Barcode", "Qty", "Brand", "Weight, g"], columns.Select(column => column.Title));
        Assert.Null(columns[0].Sort);
    }

    [Theory]
    [InlineData(false, "Brand")]
    [InlineData(true, "Weight, g")]
    public void Columns_Narrow_ShowQuantityAndOneCharacteristic(bool chooseWeight, string expected)
    {
        var columns = ProductTable.Columns(Options, SortOption.ByName, false, narrow: true, chooseWeight ? Weight : null);

        Assert.Equal(["Qty", expected], columns.Select(column => column.Title));
    }

    [Fact]
    public void Columns_MarkOnlyTheSortedOne()
    {
        var columns = ProductTable.Columns(Options, Weight, true, narrow: false, extra: null);

        var sorted = Assert.Single(columns, column => column.IsSorted);
        Assert.Equal("Weight, g ▼", sorted.Header);
        Assert.True(sorted.IsDescending);
        Assert.Contains("sorted descending", sorted.Description);
        Assert.False(ProductTable.NameColumn(Weight, true).IsSorted);
        Assert.Equal("Name ▲", ProductTable.NameColumn(SortOption.ByName, false).Header);
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
        var columns = ProductTable.Columns(Options, SortOption.ByName, false, narrow: false, extra: null);

        var row = ProductTable.Row(product, columns);

        Assert.Equal(["3017620422003", ProductTable.OutOfStock, "Ferrero", ProductTable.Missing], row.Cells.Select(cell => cell.Text));
        Assert.True(row.Cells[1].IsAlert);
        Assert.False(row.Cells[2].IsAlert);
        Assert.Equal(["Header_Barcode", "SortHeader_Quantity"], columns.Take(2).Select(column => column.AutomationId));
        Assert.True(row.CanDelete);
    }
}
