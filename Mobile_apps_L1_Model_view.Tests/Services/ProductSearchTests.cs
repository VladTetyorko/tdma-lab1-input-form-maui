using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ProductSearchTests
{
    private static readonly Guid Brand = Guid.NewGuid();

    private static readonly ProductSummary[] Products =
    [
        Item("Nutella 400 g", "3017620422003", "Ferrero"),
        Item("Nutella 500 g", "4008400402222", "Ferrero"),
        Item("Oat Biscuits", "8000500310427", "Gerble"),
        Item("Cordless Drill", "4006381333931")
    ];

    [Theory]
    [InlineData("Nutella", new[] { "Nutella 400 g", "Nutella 500 g" })]
    [InlineData("nUTELLA", new[] { "Nutella 400 g", "Nutella 500 g" })]
    [InlineData("500 g", new[] { "Nutella 500 g" })]
    [InlineData("4006381333931", new[] { "Cordless Drill" })]
    [InlineData("40063", new[] { "Cordless Drill" })]
    [InlineData("  drill  ", new[] { "Cordless Drill" })]
    [InlineData("ferrero", new[] { "Nutella 400 g", "Nutella 500 g" })]
    [InlineData("nothing like this", new string[0])]
    public void Filter_MatchesNameBarcodeAndValues_IgnoringCaseAndSurroundingSpaces(string text, string[] expected)
    {
        var found = ProductSearch.Filter(Products, text);

        Assert.Equal(expected, found.Select(match => match.Product.Name).ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Filter_WithEmptyText_ReturnsEverythingInTheSameOrder(string? text)
    {
        var found = ProductSearch.Filter(Products, text);

        Assert.Equal(Products.Select(p => p.Name), found.Select(match => match.Product.Name));
    }

    [Fact]
    public void Filter_FindsATypoAndMarksIt()
    {
        var found = ProductSearch.Filter(Products, "nutela");

        Assert.Equal(["Nutella 400 g", "Nutella 500 g"], found.Select(match => match.Product.Name));
        Assert.All(found, match => Assert.True(match.IsFuzzy && match.NameRanges.Count > 0));
    }

    [Fact]
    public void Filter_DoesNotChangeTheSource()
    {
        var source = Products.ToList();

        ProductSearch.Filter(source, "drill");

        Assert.Equal(4, source.Count);
    }

    private static ProductSummary Item(string name, string barcode, string? brand = null) =>
        new(Guid.NewGuid(), name, barcode, 1) { Values = brand is null ? [] : new Dictionary<Guid, string> { [Brand] = brand } };
}
