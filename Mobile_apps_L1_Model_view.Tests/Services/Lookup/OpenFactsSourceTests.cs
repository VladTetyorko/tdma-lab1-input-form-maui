using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Services.Lookup;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services.Lookup;

public class OpenFactsSourceTests
{
    private const string Barcode = "3017620422003";

    private const string FoundJson = """
        {"code":"3017620422003","status":1,"status_verbose":"product found","product":{
          "brands":"Ferrero, Nutella","categories_tags":["en:spreads","fr:pates-a-tartiner","en:chocolate-spreads"],
          "product_name":"Nutella","quantity":"400 g e"}}
        """;

    private readonly FakeHttpMessageHandler _handler = new();
    private readonly OpenFactsSource _lookup;

    public OpenFactsSourceTests()
    {
        _lookup = Source(OpenFactsSite.Food);
    }

    private OpenFactsSource Source(OpenFactsSite site) =>
        new(LookupHttpClient.Create(_handler), site, NullLogger<OpenFactsSource>.Instance);

    [Fact]
    public async Task Find_KnownProduct_ReturnsNameBrandQuantityAndCategory()
    {
        _handler.RespondWith(HttpStatusCode.OK, FoundJson);

        var result = await _lookup.FindAsync(Barcode);

        Assert.Equal(LookupResult.Found("Nutella", "Ferrero", "400 g e", "Chocolate spreads") with
        {
            Source = "Open Food Facts",
            Attribution = "Data: Open Food Facts (ODbL), openfoodfacts.org"
        }, result);
    }

    [Fact]
    public async Task Find_SendsOneRequestWithTheAgreedUserAgentAndBarcode()
    {
        _handler.RespondWith(HttpStatusCode.OK, FoundJson);

        await _lookup.FindAsync(Barcode);

        var request = Assert.Single(_handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal($"https://world.openfoodfacts.org/api/v2/product/{Barcode}", request.RequestUri!.GetLeftPart(UriPartial.Path));
        Assert.Equal("StockRoom/1.0 (teaching project)", request.Headers.UserAgent.ToString());
    }

    [Fact]
    public void Client_HasAFiveSecondTimeout()
    {
        using var client = LookupHttpClient.Create(_handler);

        Assert.Equal(TimeSpan.FromSeconds(5), client.Timeout);
    }

    [Fact]
    public async Task Find_OnlyNameInTheAnswer_FillsTheRestWithEmptyText()
    {
        _handler.RespondWith(HttpStatusCode.OK, """{"status":1,"product":{"product_name":"Water"}}""");

        var result = await _lookup.FindAsync(Barcode);

        Assert.Equal(("Water", "", "", ""), (result.Name, result.Brand, result.QuantityText, result.Category));
    }

    [Theory]
    [InlineData("""{"code":"3017620422003","status":0,"status_verbose":"product not found"}""")]
    [InlineData("""{"status":1,"product":{"product_name":"  "}}""")]
    [InlineData("""{"status":1}""")]
    [InlineData("[]")]
    public async Task Find_NoUsableProduct_IsNotFound(string json)
    {
        _handler.RespondWith(HttpStatusCode.OK, json);

        Assert.Equal(LookupStatus.NotFound, (await _lookup.FindAsync(Barcode)).Status);
    }

    [Fact]
    public async Task Find_Http404_IsNotFound()
    {
        _handler.RespondWith(HttpStatusCode.NotFound, """{"status":0}""");

        Assert.Equal(LookupStatus.NotFound, (await _lookup.FindAsync(Barcode)).Status);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("""{"status":1,"product":""")]
    [InlineData("")]
    public async Task Find_MalformedJson_IsFailedNotACrash(string json)
    {
        _handler.RespondWith(HttpStatusCode.OK, json);

        Assert.Equal(LookupStatus.Failed, (await _lookup.FindAsync(Barcode)).Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Find_ServerError_IsFailed(HttpStatusCode status)
    {
        _handler.RespondWith(status, "<html>busy</html>");

        Assert.Equal(LookupStatus.Failed, (await _lookup.FindAsync(Barcode)).Status);
    }

    [Fact]
    public async Task Find_NoNetwork_IsOffline()
    {
        _handler.Throw(new HttpRequestException("no route to host"));

        Assert.Equal(LookupStatus.Offline, (await _lookup.FindAsync(Barcode)).Status);
    }

    [Fact]
    public async Task Find_Timeout_IsOffline()
    {
        _handler.Throw(new TaskCanceledException("The request timed out.", new TimeoutException()));

        Assert.Equal(LookupStatus.Offline, (await _lookup.FindAsync(Barcode)).Status);
    }

    [Fact]
    public async Task Find_CancelledByTheCaller_IsStillACancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _handler.Throw(new TaskCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _lookup.FindAsync(Barcode, cts.Token));
    }

    [Theory]
    [InlineData("Open Beauty Facts", "openbeautyfacts.org")]
    [InlineData("Open Pet Food Facts", "openpetfoodfacts.org")]
    [InlineData("Open Products Facts", "openproductsfacts.org")]
    public async Task TheSisterDatabases_UseTheSameApiOnTheirOwnHost(string name, string host)
    {
        _handler.RespondWith(HttpStatusCode.OK, FoundJson);

        var result = await Source(OpenFactsSite.All.Single(site => site.Name == name)).FindAsync(Barcode);

        Assert.Equal((name, $"Data: {name} (ODbL), {host}"), (result.Source, result.Attribution));
        Assert.Equal($"https://world.{host}/api/v2/product/{Barcode}",
            _handler.Requests.Single().RequestUri!.GetLeftPart(UriPartial.Path));
    }

    [Fact]
    public async Task Find_ATooLongName_IsCutToWhatAProductMayHave()
    {
        _handler.RespondWith(HttpStatusCode.OK, "{\"status\":1,\"product\":{\"product_name\":\"" + new string('a', 300) + "\"}}");

        Assert.Equal(ProductValidator.MaxNameLength, (await _lookup.FindAsync(Barcode)).Name.Length);
    }
}
