using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class DeepLinkParserTests
{
    private const string Barcode = "4820001000016";

    [Fact]
    public void Parse_ValidLink_ReturnsTheBarcode()
    {
        var link = DeepLinkParser.Parse($"stockroom://product/{Barcode}");

        Assert.Equal(new ProductDeepLink(Barcode), link);
    }

    [Theory]
    [InlineData("STOCKROOM://product/4820001000016")]
    [InlineData("StockRoom://PRODUCT/4820001000016")]
    [InlineData("stockroom://Product/4820001000016")]
    public void Parse_SchemeAndHostIgnoreCase(string link)
    {
        Assert.Equal(new ProductDeepLink(Barcode), DeepLinkParser.Parse(link));
    }

    [Fact]
    public void Parse_DecodesPercentEscapes()
    {
        var link = DeepLinkParser.Parse("stockroom://product/ABC%20123");

        Assert.Equal("ABC 123", link!.Barcode);
    }

    [Fact]
    public void Parse_QueryAndFragmentAreIgnored()
    {
        var link = DeepLinkParser.Parse($"stockroom://product/{Barcode}?from=mail&x=1#top");

        Assert.Equal(new ProductDeepLink(Barcode), link);
    }

    [Fact]
    public void Parse_TrimsSurroundingSpaces()
    {
        var link = DeepLinkParser.Parse($"  stockroom://product/{Barcode}  ");

        Assert.Equal(new ProductDeepLink(Barcode), link);
    }

    [Theory]
    [InlineData("stockroom://product/4820001000016/extra")]
    [InlineData("stockroom://product/4820001000016/a/b")]
    public void Parse_ExtraPathSegments_ReturnsNull(string link)
    {
        Assert.Null(DeepLinkParser.Parse(link));
    }

    [Theory]
    [InlineData("stockroom://product/")]
    [InlineData("stockroom://product")]
    [InlineData("stockroom://product/%20%20")]
    public void Parse_EmptyBarcode_ReturnsNull(string link)
    {
        Assert.Null(DeepLinkParser.Parse(link));
    }

    [Theory]
    [InlineData("https://product/4820001000016")]
    [InlineData("stockroom2://product/4820001000016")]
    public void Parse_WrongScheme_ReturnsNull(string link)
    {
        Assert.Null(DeepLinkParser.Parse(link));
    }

    [Theory]
    [InlineData("stockroom://order/4820001000016")]
    [InlineData("stockroom://products/4820001000016")]
    public void Parse_WrongHost_ReturnsNull(string link)
    {
        Assert.Null(DeepLinkParser.Parse(link));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a link")]
    [InlineData("stockroom:")]
    [InlineData("://product/4820001000016")]
    [InlineData("product/4820001000016")]
    public void Parse_NullOrGarbage_ReturnsNull(string? link)
    {
        Assert.Null(DeepLinkParser.Parse(link));
    }

    [Fact]
    public void ForProduct_BuildsTheLinkTheParserReads()
    {
        Assert.Equal("stockroom://product/4820001000016", DeepLinkParser.ForProduct(Barcode));
    }

    [Theory]
    [InlineData("4820001000016")]
    [InlineData("ABC 123")]
    [InlineData("A/B?C#D")]
    public void ForProduct_RoundTripsThroughParse(string barcode)
    {
        var link = DeepLinkParser.Parse(DeepLinkParser.ForProduct(barcode));

        Assert.Equal(barcode, link?.Barcode);
    }
}
