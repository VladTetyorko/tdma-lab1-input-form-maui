using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ScanPayloadParserTests
{
    private const string Nutella = "3017620422003";

    [Theory]
    [InlineData("3017620422003", "3017620422003")]
    [InlineData("  3017620422003 \n", "3017620422003")]
    [InlineData("stockroom://product/3017620422003", "3017620422003")]
    [InlineData("STOCKROOM://Product/3017620422003?from=qr", "3017620422003")]
    [InlineData("  stockroom://product/3017620422003  ", "3017620422003")]
    // UPC-A: 12 digits with a valid check digit, padded with a leading zero
    [InlineData("036000291452", "0036000291452")]
    [InlineData("stockroom://product/036000291452", "0036000291452")]
    public void Parse_AcceptedPayloads_GiveTheEan13(string raw, string expected)
    {
        var result = ScanPayloadParser.Parse(raw);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(expected, result.Value);
        Assert.True(Ean13.IsValid(result.Value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_Empty_AsksForABarcode(string? raw)
    {
        var result = ScanPayloadParser.Parse(raw);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScanPayloadParser.EmptyMessage, result.Error);
    }

    [Theory]
    [InlineData("3017620422004")] // wrong EAN-13 check digit
    [InlineData("036000291453")] // wrong UPC-A check digit
    [InlineData("30176204220")] // 11 digits
    [InlineData("301762042200312")] // 15 digits
    [InlineData("ABC")]
    [InlineData("https://example.com/3017620422003")]
    [InlineData("stockroom://product/ABC")]
    [InlineData("stockroom://product/")]
    [InlineData("stockroom://product/3017620422003/extra")]
    [InlineData("stockroom://warehouse/3017620422003")]
    [InlineData("WIFI:S:home;T:WPA;P:secret;;")]
    public void Parse_AnythingElse_IsNotAStockRoomCode(string raw)
    {
        var result = ScanPayloadParser.Parse(raw);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScanPayloadParser.NotStockRoomCodeMessage, result.Error);
    }

    [Fact]
    public void Parse_DeepLinkBuiltByTheApp_RoundTrips()
    {
        var result = ScanPayloadParser.Parse(DeepLinkParser.ForProduct(Nutella));

        Assert.Equal(Nutella, result.Value);
    }
}
