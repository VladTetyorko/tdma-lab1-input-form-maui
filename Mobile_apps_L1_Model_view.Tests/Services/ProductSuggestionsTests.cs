using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ProductSuggestionsTests
{
    private static readonly Characteristic Brand = new() { Name = "Brand" };
    private static readonly Characteristic Size = new() { Name = "Size" };
    private static readonly Characteristic Origin = new() { Name = "Origin" };
    private static readonly IReadOnlyList<Characteristic> Characteristics = [Brand, Size, Origin];

    private static ProductSummary Product(string name, string code, int quantity = 0) =>
        new(Guid.NewGuid(), name, code, quantity);

    private static IEnumerable<string> Names(string text, int max, params ProductSummary[] products) =>
        ProductSuggestions.Find(products, Characteristics, text, max).Select(suggestion => suggestion.Title);

    [Fact]
    public void Ranking_ExactCodeThenPrefixesThenAnywhere()
    {
        var products = new[]
        {
            Product("Anywhere", "9912300000000"),
            Product("Oil 123 extra", "5000000000000"),
            Product("123 glue", "6000000000000"),
            Product("Code prefix", "1230000000000"),
            Product("Exact", "123")
        };

        Assert.Equal(["Exact", "Code prefix", "123 glue", "Oil 123 extra", "Anywhere"], Names("123", 8, products));
    }

    [Fact]
    public void Ties_AreOrderedByName_AndTheListIsLimited()
    {
        var products = new[] { Product("Nut C", "3"), Product("Nut A", "1"), Product("Nut B", "2") };

        Assert.Equal(["Nut A", "Nut B"], Names("  nut ", 2, products));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("n", false)]
    [InlineData("nu", true)]
    [InlineData("3", true)]
    public void OneDigitSearches_OneLetterDoesNot(string text, bool searchable)
    {
        Assert.Equal(searchable, ProductSuggestions.IsSearchable(text));
        Assert.Equal(searchable, Names(text, 5, Product("Nut 3", "3017620422003")).Any());
    }

    [Fact]
    public void CharacteristicValues_AreShownButNotSearched()
    {
        var product = Product("Nutella", "3017620422003", 24) with
        {
            Values = new Dictionary<Guid, string> { [Size.Id] = "400 g", [Brand.Id] = "Ferrero", [Origin.Id] = "Italy" }
        };

        var suggestion = Assert.Single(ProductSuggestions.Find([product], Characteristics, "nut", 5));

        Assert.Equal("Ferrero · 400 g", suggestion.Details);
        Assert.Equal("Ferrero · 400 g · 3017620422003", suggestion.DetailsAndCode);
        Assert.Equal("24 on hand", suggestion.StockText);
        Assert.False(suggestion.IsExactCode);
        Assert.Empty(ProductSuggestions.Find([product], Characteristics, "Ferrero", 5));
    }

    [Fact]
    public void NoStockAndNoValues_SaySo()
    {
        var suggestion = ProductSuggestions.Find([Product("Nutella", "3017620422003")], Characteristics, "3017620422003", 5)[0];

        Assert.Equal("Out of stock", suggestion.StockText);
        Assert.Equal("3017620422003", suggestion.DetailsAndCode);
        Assert.True(suggestion.IsExactCode);
    }
}
