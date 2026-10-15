using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ProductSorterTests
{
    private static readonly Characteristic Color = new() { Name = "Color", ValueType = CharacteristicValueType.Text };
    private static readonly Characteristic Shelf = new() { Name = "Shelf", ValueType = CharacteristicValueType.Number };

    private static readonly SortOption ByColor = SortOption.ByCharacteristic(Color);
    private static readonly SortOption ByShelf = SortOption.ByCharacteristic(Shelf);

    [Fact]
    public void Sort_ByName_IgnoresCase()
    {
        var products = new[] { Item("banana"), Item("Apple"), Item("cherry") };

        var sorted = ProductSorter.Sort(products, SortOption.ByName, descending: false);

        Assert.Equal(new[] { "Apple", "banana", "cherry" }, Names(sorted));
    }

    [Fact]
    public void Sort_ByName_Descending()
    {
        var products = new[] { Item("banana"), Item("Apple"), Item("cherry") };

        var sorted = ProductSorter.Sort(products, SortOption.ByName, descending: true);

        Assert.Equal(new[] { "cherry", "banana", "Apple" }, Names(sorted));
    }

    [Theory]
    [InlineData(false, new[] { "Saw", "Drill", "Hammer" })]
    [InlineData(true, new[] { "Hammer", "Drill", "Saw" })]
    public void Sort_ByQuantity_OrdersByStock(bool descending, string[] expected)
    {
        var products = new[] { Item("Drill", 5), Item("Hammer", 9), Item("Saw", 1) };

        var sorted = ProductSorter.Sort(products, SortOption.ByQuantity, descending);

        Assert.Equal(expected, Names(sorted));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Sort_TiesAreOrderedByName(bool descending)
    {
        var products = new[] { Item("Zeta", 3), Item("Alpha", 3) };

        var sorted = ProductSorter.Sort(products, SortOption.ByQuantity, descending);

        Assert.Equal(new[] { "Alpha", "Zeta" }, Names(sorted));
    }

    [Fact]
    public void Sort_TextCharacteristic_IsCaseInsensitive()
    {
        var products = new[]
        {
            Item("A", values: new() { [Color.Id] = "Red" }),
            Item("B", values: new() { [Color.Id] = "blue" }),
            Item("C", values: new() { [Color.Id] = "GREEN" })
        };

        var sorted = ProductSorter.Sort(products, ByColor, descending: false);

        Assert.Equal(new[] { "B", "C", "A" }, Names(sorted));
    }

    [Fact]
    public void Sort_NumberCharacteristic_ComparesNumbersNotText()
    {
        var products = new[]
        {
            Item("Ten", values: new() { [Shelf.Id] = "10" }),
            Item("Two", values: new() { [Shelf.Id] = "2" }),
            Item("Nine", values: new() { [Shelf.Id] = "9" })
        };

        var sorted = ProductSorter.Sort(products, ByShelf, descending: false);

        Assert.Equal(new[] { "Two", "Nine", "Ten" }, Names(sorted));
    }

    [Fact]
    public void Sort_NumberCharacteristic_Descending()
    {
        var products = new[]
        {
            Item("Two", values: new() { [Shelf.Id] = "2" }),
            Item("Ten", values: new() { [Shelf.Id] = "10" }),
            Item("Nine", values: new() { [Shelf.Id] = "9" })
        };

        var sorted = ProductSorter.Sort(products, ByShelf, descending: true);

        Assert.Equal(new[] { "Ten", "Nine", "Two" }, Names(sorted));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Sort_ProductsWithoutUsableValue_AreLastInBothDirections(bool descending)
    {
        var products = new[]
        {
            Item("Absent"),
            Item("Blank", values: new() { [Shelf.Id] = "   " }),
            Item("Garbage", values: new() { [Shelf.Id] = "tall" }),
            Item("Low", values: new() { [Shelf.Id] = "1" }),
            Item("High", values: new() { [Shelf.Id] = "7" })
        };

        var sorted = ProductSorter.Sort(products, ByShelf, descending);

        var names = Names(sorted);
        Assert.Equal(descending ? "High" : "Low", names[0]);
        Assert.Equal(descending ? "Low" : "High", names[1]);
        Assert.Equal(new[] { "Absent", "Blank", "Garbage" }, names[2..]);
    }

    [Fact]
    public void Sort_TextCharacteristic_BlankValueIsLastInDescendingOrder()
    {
        var products = new[]
        {
            Item("Blank", values: new() { [Color.Id] = " " }),
            Item("Red", values: new() { [Color.Id] = "Red" }),
            Item("Absent")
        };

        var sorted = ProductSorter.Sort(products, ByColor, descending: true);

        Assert.Equal(new[] { "Red", "Absent", "Blank" }, Names(sorted));
    }

    [Fact]
    public void Sort_NumberTiesAreOrderedByName()
    {
        var products = new[]
        {
            Item("Zeta", values: new() { [Shelf.Id] = "5" }),
            Item("Alpha", values: new() { [Shelf.Id] = "5.0" })
        };

        var sorted = ProductSorter.Sort(products, ByShelf, descending: true);

        Assert.Equal(new[] { "Alpha", "Zeta" }, Names(sorted));
    }

    [Fact]
    public void All_ReturnsNameQuantityThenCharacteristicsInGivenOrder()
    {
        var options = SortOption.All([Color, Shelf]);

        Assert.Equal(new[] { "Name", "Quantity", "Color", "Shelf" }, options.Select(o => o.Title));
        Assert.Equal(SortKind.Name, options[0].Kind);
        Assert.Equal(SortKind.Quantity, options[1].Kind);
        Assert.Equal(Color.Id, options[2].CharacteristicId);
        Assert.Equal(CharacteristicValueType.Number, options[3].ValueType);
    }

    [Fact]
    public void All_WithoutCharacteristics_ReturnsOnlyNameAndQuantity()
    {
        Assert.Equal(new[] { "Name", "Quantity" }, SortOption.All([]).Select(o => o.Title));
    }

    [Fact]
    public void SortOption_ToString_ReturnsTitleForThePicker()
    {
        Assert.Equal("Shelf", ByShelf.ToString());
    }

    private static ProductSummary Item(
        string name, int quantity = 0, Dictionary<Guid, string>? values = null) =>
        new(Guid.NewGuid(), name, "4006381333931", quantity) { Values = values ?? [] };

    private static string[] Names(IEnumerable<ProductSummary> products) => products.Select(p => p.Name).ToArray();
}
