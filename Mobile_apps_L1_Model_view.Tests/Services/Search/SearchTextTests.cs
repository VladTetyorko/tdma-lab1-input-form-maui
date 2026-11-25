using Mobile_apps_L1_Model_view.Services.Search;

namespace Mobile_apps_L1_Model_view.Tests.Services.Search;

public class SearchTextTests
{
    [Theory]
    [InlineData("Caf\u00e9", "cafe")]
    [InlineData("  Coca-Cola,  1.5 L ", "coca cola 1 5 l")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalises_CaseDiacriticsAndPunctuation(string? original, string expected) =>
        Assert.Equal(expected, SearchText.From(original).Value);

    [Fact]
    public void Ranges_PointBackIntoTheOriginal()
    {
        const string original = "M\u00fcsli  Bar";
        var text = SearchText.From(original);

        Assert.Equal("musli bar", text.Value);
        var range = text.Original(text.Value.IndexOf("bar", StringComparison.Ordinal), 3);
        Assert.Equal("Bar", original.Substring(range.Start, range.Length));
        Assert.Equal(new TextRange(1, 1), text.Original(1, 1));
    }
}
