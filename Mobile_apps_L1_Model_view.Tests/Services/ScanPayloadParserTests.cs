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

    [Theory]
    [InlineData("96385074", "0000096385074")] // EAN-8
    [InlineData("04252614", "0042100005264")] // UPC-E, last digit 1
    [InlineData("0140328726", "9780140328721")] // ISBN-10
    [InlineData("097522980X", "9780975229804")] // ISBN-10 with X
    [InlineData("097522980x", "9780975229804")]
    [InlineData("0-14-032872-6", "9780140328721")]
    [InlineData("978-0-14-032872-1", "9780140328721")]
    [InlineData("978 0140328721", "9780140328721")]
    [InlineData("stockroom://product/96385074", "0000096385074")]
    [InlineData("3017620422003", "3017620422003")] // what is stored stays as it is
    public void Parse_OtherForms_BecomeTheStoredEan13(string raw, string expected)
    {
        var result = ScanPayloadParser.Parse(raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("96385075")] // wrong EAN-8 check digit
    [InlineData("04252615")] // wrong UPC-E check digit
    [InlineData("0140328727")] // wrong ISBN-10 check digit
    [InlineData("01403X8726")] // X not at the end
    [InlineData("97522980X")] // too short
    [InlineData("-3017620422003")]
    [InlineData("1234567")]
    public void Parse_BadShortForms_AreRejected(string raw) => Assert.False(ScanPayloadParser.Parse(raw).IsSuccess);

    [Fact]
    public void Parse_DeepLinkBuiltByTheApp_RoundTrips()
    {
        var result = ScanPayloadParser.Parse(DeepLinkParser.ForProduct(Nutella));

        Assert.Equal(Nutella, result.Value);
    }
}
