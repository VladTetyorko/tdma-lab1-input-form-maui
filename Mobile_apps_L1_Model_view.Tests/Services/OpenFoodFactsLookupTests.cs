using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class OpenFoodFactsLookupTests
{
    private const string Barcode = "3017620422003";

    private const string FoundJson = """
        {"code":"3017620422003","status":1,"status_verbose":"product found","product":{
          "brands":"Ferrero, Nutella","categories_tags":["en:spreads","fr:pates-a-tartiner","en:chocolate-spreads"],
          "product_name":"Nutella","quantity":"400 g e"}}
        """;

    private readonly FakeHttpMessageHandler _handler = new();
    private readonly OpenFoodFactsLookup _lookup;

    public OpenFoodFactsLookupTests()
    {
        _lookup = new OpenFoodFactsLookup(
            OpenFoodFactsLookup.CreateClient(_handler), NullLogger<OpenFoodFactsLookup>.Instance);
    }

    [Fact]
    public async Task Find_KnownProduct_ReturnsNameBrandQuantityAndCategory()
    {
        _handler.RespondWith(HttpStatusCode.OK, FoundJson);

        var result = await _lookup.FindAsync(Barcode);

        Assert.Equal(LookupResult.Found("Nutella", "Ferrero", "400 g e", "Chocolate spreads"), result);
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
        using var client = OpenFoodFactsLookup.CreateClient(_handler);

        Assert.Equal(TimeSpan.FromSeconds(5), client.Timeout);
    }

    [Fact]
    public async Task Find_OnlyNameInTheAnswer_FillsTheRestWithEmptyText()
    {
        _handler.RespondWith(HttpStatusCode.OK, """{"status":1,"product":{"product_name":"Water"}}""");

        var result = await _lookup.FindAsync(Barcode);

        Assert.Equal(LookupResult.Found("Water", "", "", ""), result);
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

    [Fact]
    public async Task Container_BuildsTheLookupOverTheRegisteredHandler()
    {
        using var provider = new ServiceCollection().AddStockRoomCore().AddFakePlatform().BuildServiceProvider();
        var network = provider.GetRequiredService<FakeHttpMessageHandler>();
        network.RespondWith(HttpStatusCode.OK, FoundJson);

        var result = await provider.GetRequiredService<IProductLookup>().FindAsync(Barcode);

        Assert.True(result.IsFound);
        Assert.Single(network.Requests);
    }

    [Fact]
    public async Task Container_WithoutANetwork_AnswersOfflineInsteadOfThrowing()
    {
        using var provider = new ServiceCollection().AddStockRoomCore().AddFakePlatform().BuildServiceProvider();

        var result = await provider.GetRequiredService<IProductLookup>().FindAsync(Barcode);

        Assert.Equal(LookupStatus.Offline, result.Status);
    }

    [Fact]
    public void Attribution_NamesTheSourceAndLicence()
    {
        Assert.Equal("Data: Open Food Facts (ODbL)", _lookup.Attribution);
    }
}
