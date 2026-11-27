using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Services.Search;

namespace Mobile_apps_L1_Model_view.Tests.Services.Search;

public class ProductMatcherTests
{
    private static readonly ProductSummary Nutella400 = Product("Nutella 400 g", "3017620422003", 12);
    private static readonly ProductSummary Nutella500 = Product("Nutella 500 g", "4008400402222", 0);
    private static readonly ProductSummary Skyr = Product("Danone Skyr 140 g", "3033491484385", 4);
    private static readonly ProductSummary Oats = Product("Oat Biscuits", "8000500310427", 3);
    private static readonly ProductSummary[] All = [Nutella400, Nutella500, Skyr, Oats];

    private static ProductSummary Product(string name, string code, int quantity = 1) =>
        new(Guid.NewGuid(), name, code, quantity);

    private static string[] Found(string text, bool inStockFirst = false) =>
        [.. ProductMatcher.Rank(All, text, inStockFirst).Select(match => match.Product.Name)];

    [Theory]
    [InlineData("3017620422003", "Nutella 400 g", 1000)] // D0 equal
    [InlineData("30176", "Nutella 400 g", 800)]          // D1 prefix
    [InlineData("003", "Nutella 400 g", 700)]            // D2 the last digits
    [InlineData("0500310", "Oat Biscuits", 500)]         // D3 inside
    [InlineData("nutella 4", null, 0)]
    [InlineData("oat", "Oat Biscuits", 600)]             // L0 the name starts with it
    [InlineData("sky", "Danone Skyr 140 g", 500)]        // L1 a word starts with it
    [InlineData("iscu", "Oat Biscuits", 350)]            // L2 inside the name
    public void ExactTiers(string text, string? expected, double score)
    {
        var matches = ProductMatcher.Rank(All, text);

        if (expected is null)
        {
            Assert.All(matches, match => Assert.False(match.IsFuzzy));
            return;
        }

        Assert.Equal((expected, score, false), (matches[0].Product.Name, matches[0].Score, matches[0].IsFuzzy));
    }

    [Theory]
    [InlineData("42")]   // two digits at the end of a code do not match
    [InlineData("nutnut")]
    [InlineData("3017620422004")] // a code is never similar
    public void NothingMatches(string text) =>
        Assert.DoesNotContain(ProductMatcher.Rank([Product("Plain", "1000000000042"), .. All], text),
            match => match.Product.Name.StartsWith("Nutella", StringComparison.Ordinal) || match.Product.Name == "Plain"
                && text != "42");

    [Theory]
    [InlineData("nutela")]  // one letter missing
    [InlineData("nutlela")] // one letter too many
    [InlineData("nutlela 400")]
    public void ATypo_IsFoundAsSimilar(string text)
    {
        var matches = ProductMatcher.Rank(All, text);

        Assert.Equal("Nutella 400 g", matches[0].Product.Name);
        Assert.True(matches[0].IsFuzzy);
    }

    [Fact]
    public void EveryWordMustMatch()
    {
        Assert.Equal(["Nutella 500 g"], Found("nut 500"));
        Assert.Equal(["Nutella 500 g"], Found("500 g"));
        Assert.Equal(["Nutella 400 g"], Found("nutella 400")); // not the 500 g whose barcode starts with 400
        Assert.Empty(Found("nut zzz"));
    }

    [Fact]
    public void ShortQueries_StayNarrow()
    {
        Assert.Equal(["Nutella 400 g", "Nutella 500 g"], Found("n"));
        Assert.DoesNotContain(ProductMatcher.Rank(All, "nu"), match => match.IsFuzzy);
        Assert.Empty(Found("el"));
        Assert.Empty(Found("  "));
    }

    [Fact]
    public void BarcodeTiers_ScoreAboveNameTiers()
    {
        Assert.Equal(["Nutella 500 g", "Nutella 400 g"], Found("40"));
    }

    [Fact]
    public void WhenIssuing_ProductsInStockComeFirst()
    {
        ProductSummary[] products = [Product("Tea A", "1", 0), Product("Tea B", "2", 5)];

        Assert.Equal(["Tea B", "Tea A"],
            ProductMatcher.Rank(products, "tea", inStockFirst: true).Select(match => match.Product.Name));
        Assert.Equal(["Tea A", "Tea B"], ProductMatcher.Rank(products, "tea").Select(match => match.Product.Name));
    }

    [Fact]
    public void CharacteristicValues_AreSearchedOnlyWhenAsked()
    {
        var product = Product("Hazelnut spread", "5") with
        {
            Values = new Dictionary<Guid, string> { [Guid.NewGuid()] = "Ferrero" }
        };

        Assert.Empty(ProductMatcher.Rank([product], "ferr"));
        Assert.Single(ProductMatcher.Rank([product], "ferr", includeValues: true));
    }

    [Fact]
    public void Highlight_MarksTheMatchedParts()
    {
        Assert.Equal([new TextRange(2, 3)], ProductMatcher.Rank([Nutella400], "tel")[0].NameRanges);
        Assert.Equal([new TextRange(10, 3)], ProductMatcher.Rank([Nutella400], "003")[0].CodeRanges);

        // "nutela": the shared n-grams, merged into one run at the start of the word
        var range = Assert.Single(ProductMatcher.Rank([Nutella400], "nutela")[0].NameRanges);
        Assert.Equal(0, range.Start);
        Assert.InRange(range.Length, 4, 7);
    }

    [Fact]
    public void Highlight_StaysOnTheOriginalWithDiacritics()
    {
        var product = Product("M\u00fcsli  Cr\u00e8me", "7");

        var range = Assert.Single(ProductMatcher.Rank([product], "creme")[0].NameRanges);

        Assert.Equal("Cr\u00e8me", product.Name.Substring(range.Start, range.Length));
    }

    [Theory]
    [InlineData("abcd", "abcd", 0)]
    [InlineData("abcd", "abdc", 1)]
    [InlineData("nutela", "nutell", 1)]
    [InlineData("abc", "", 3)]
    public void Distance_CountsSwapsAsOne(string a, string b, int expected) =>
        Assert.Equal(expected, FuzzyText.Distance(a, b));
}
