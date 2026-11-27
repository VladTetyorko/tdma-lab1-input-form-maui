using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Services.Lookup;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services.Lookup;

// The chain as the container builds it, over a fake network
public sealed class ProductLookupChainTests : IDisposable
{
    private const string Food = "3017620422003";
    private const string Isbn = "9780140328721";

    private static readonly string[] OpenFacts =
    [
        "world.openfoodfacts.org", "world.openbeautyfacts.org", "world.openpetfoodfacts.org", "world.openproductsfacts.org"
    ];

    private readonly ServiceProvider _provider = new ServiceCollection().AddStockRoomCore().AddFakePlatform().BuildServiceProvider();
    private readonly FakeHttpMessageHandler _network;
    private readonly IProductLookup _lookup;

    public ProductLookupChainTests()
    {
        _network = _provider.GetRequiredService<FakeHttpMessageHandler>();
        _lookup = _provider.GetRequiredService<IProductLookup>();
    }

    public void Dispose() => _provider.Dispose();

    private IEnumerable<string> Hosts => _network.Requests.Select(request => request.RequestUri!.Host);

    private static HttpResponseMessage Answer(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    // Only the given host knows the code
    private void OnlyKnownBy(string host, Func<HttpRequestMessage, HttpResponseMessage>? others = null) =>
        _network.Respond(request => request.RequestUri!.Host == host
            ? Answer(host == "openlibrary.org"
                ? """{"ISBN:9780140328721":{"title":"Fantastic Mr. Fox"}}"""
                : """{"status":1,"product":{"product_name":"Thing"}}""")
            : others?.Invoke(request) ?? Answer(request.RequestUri.Host == "openlibrary.org" ? "{}" : """{"status":0}"""));

    [Fact]
    public async Task ABook_IsAskedAtOpenLibraryFirst_AndNobodyElse()
    {
        OnlyKnownBy("openlibrary.org");

        var result = await _lookup.FindAsync(Isbn);

        Assert.Equal("Open Library", result.Source);
        Assert.Equal(["openlibrary.org"], Hosts);
    }

    [Fact]
    public async Task OtherCodes_AreAskedInTheOpenFactsOrder_UntilOneKnowsIt()
    {
        OnlyKnownBy("world.openpetfoodfacts.org");

        var result = await _lookup.FindAsync(Food);

        Assert.Equal("Open Pet Food Facts", result.Source);
        Assert.Equal(OpenFacts[..3], Hosts);
    }

    [Fact]
    public async Task SourcesThatFail_DoNotBlockTheOthers()
    {
        OnlyKnownBy("world.openproductsfacts.org", request => request.RequestUri!.Host switch
        {
            "world.openfoodfacts.org" => throw new HttpRequestException("down"),
            "world.openbeautyfacts.org" => throw new TaskCanceledException("slow", new TimeoutException()),
            _ => Answer("busy", HttpStatusCode.ServiceUnavailable)
        });

        var result = await _lookup.FindAsync(Food);

        Assert.Equal("Open Products Facts", result.Source);
    }

    [Fact]
    public async Task NobodyKnowsIt_IsNotFound_AfterAskingEveryone()
    {
        OnlyKnownBy("nowhere");

        Assert.Equal(LookupStatus.NotFound, (await _lookup.FindAsync(Isbn)).Status);
        Assert.Equal(["openlibrary.org", .. OpenFacts], Hosts);
    }

    [Fact]
    public async Task NoNetworkAtAll_IsOffline()
    {
        Assert.Equal(LookupStatus.Offline, (await _lookup.FindAsync(Food)).Status);
    }

    [Fact]
    public async Task SomeUnanswered_AndTheRestWithoutIt_IsFailed_NotNotFound()
    {
        OnlyKnownBy("nowhere", request => request.RequestUri!.Host == "world.openfoodfacts.org"
            ? Answer("", HttpStatusCode.TooManyRequests)
            : Answer("""{"status":0}""", HttpStatusCode.NotFound));

        Assert.Equal(LookupStatus.Failed, (await _lookup.FindAsync(Food)).Status);
    }

    [Fact]
    public async Task AHit_IsRemembered_AMissIsAskedAgain()
    {
        OnlyKnownBy("world.openfoodfacts.org");
        await _lookup.FindAsync(Food);
        await _lookup.FindAsync(Food);
        Assert.Single(_network.Requests);

        OnlyKnownBy("nowhere");
        await _lookup.FindAsync("4008400402222");
        await _lookup.FindAsync("4008400402222");
        Assert.Equal(1 + 2 * OpenFacts.Length, _network.Requests.Count);
    }

    [Fact]
    public async Task Cancellation_StopsTheChain()
    {
        using var cancel = new CancellationTokenSource();
        _network.Respond(_ =>
        {
            cancel.Cancel();
            return Answer("""{"status":0}""");
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _lookup.FindAsync(Food, cancel.Token));
        Assert.Single(_network.Requests);
    }
}
