using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Services.Lookup;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services.Lookup;

public class OpenLibrarySourceTests
{
    private const string Isbn = "9780140328721";

    // Cut down from a real answer of the Books API
    private const string FoundJson = """
        {"ISBN:9780140328721":{"title":"Fantastic Mr. Fox","authors":[{"name":"Roald Dahl"}],
          "publishers":[{"name":"Puffin"}],"number_of_pages":96}}
        """;

    private readonly FakeHttpMessageHandler _handler = new();
    private readonly OpenLibrarySource _source;

    public OpenLibrarySourceTests()
    {
        _source = new OpenLibrarySource(LookupHttpClient.Create(_handler), NullLogger<OpenLibrarySource>.Instance);
    }

    [Fact]
    public async Task Find_KnownBook_ReturnsTitleAuthorsAndPublisher_WithOneRequest()
    {
        _handler.RespondWith(HttpStatusCode.OK, FoundJson);

        var result = await _source.FindAsync(Isbn);

        Assert.Equal(("Fantastic Mr. Fox", "Roald Dahl", "Puffin", "Book"),
            (result.Name, result.Authors, result.Publisher, result.Category));
        Assert.Equal("Roald Dahl · Puffin", result.Details);
        Assert.Equal(("Open Library", "Data: Open Library, openlibrary.org"), (result.Source, result.Attribution));
        var request = Assert.Single(_handler.Requests);
        Assert.Equal($"https://openlibrary.org/api/books?bibkeys=ISBN:{Isbn}&format=json&jscmd=data",
            request.RequestUri!.ToString());
        Assert.Equal("StockRoom/1.0 (teaching project)", request.Headers.UserAgent.ToString());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"ISBN:9780140328721":{"title":""}}""")]
    public async Task Find_UnknownIsbn_IsNotFound(string json)
    {
        _handler.RespondWith(HttpStatusCode.OK, json);

        Assert.Equal(LookupStatus.NotFound, (await _source.FindAsync(Isbn)).Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "[1]")]
    [InlineData(HttpStatusCode.OK, "<html>")]
    [InlineData(HttpStatusCode.TooManyRequests, "")]
    public async Task Find_UnusableAnswer_IsFailed(HttpStatusCode status, string body)
    {
        _handler.RespondWith(status, body);

        Assert.Equal(LookupStatus.Failed, (await _source.FindAsync(Isbn)).Status);
    }

    [Theory]
    [InlineData("9780140328721", true)]
    [InlineData("9791234567896", true)]
    [InlineData("3017620422003", false)]
    [InlineData("9771234567003", false)]
    public void Covers_OnlyIsbnCodes(string barcode, bool covered) => Assert.Equal(covered, _source.Covers(barcode));
}
