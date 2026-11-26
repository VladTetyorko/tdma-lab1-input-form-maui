using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services.Lookup;

// Books by ISBN from Open Library: one request to the Books API (jscmd=data) gives the title, authors and publishers
// of the edition. Only EAN-13 codes that are ISBNs (prefix 978 or 979) are asked
public sealed class OpenLibrarySource(HttpClient http, ILogger<OpenLibrarySource> logger) : HttpLookupSource(http, logger)
{
    public const string BookCategory = "Book";

    public override string Name => "Open Library";

    protected override string Attribution => "Data: Open Library, openlibrary.org";

    public override bool Covers(string barcode) =>
        barcode.StartsWith("978", StringComparison.Ordinal) || barcode.StartsWith("979", StringComparison.Ordinal);

    protected override Uri Address(string barcode) => new(
        $"https://openlibrary.org/api/books?bibkeys=ISBN:{Uri.EscapeDataString(barcode)}&format=json&jscmd=data");

    protected override LookupResult Read(JsonElement root, string barcode)
    {
        // An unknown ISBN is an empty object
        if (!root.TryGetProperty($"ISBN:{barcode}", out var book))
        {
            return LookupResult.NotFound();
        }

        var title = Text(book, "title");
        return title.Length == 0
            ? LookupResult.NotFound()
            : LookupResult.Found(title, string.Empty, string.Empty, BookCategory) with
            {
                Authors = Names(book, "authors"),
                Publisher = Names(book, "publishers")
            };
    }

    // "authors": [{ "name": "..." }]
    private static string Names(JsonElement book, string property) =>
        book.TryGetProperty(property, out var list) && list.ValueKind == JsonValueKind.Array
            ? string.Join(", ", list.EnumerateArray().Select(item => Text(item, "name")).Where(name => name.Length > 0))
            : string.Empty;
}
