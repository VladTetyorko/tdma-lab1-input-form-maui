using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// A product database whose answer is set by the test; remembers the barcodes asked for
public sealed class FakeProductLookup : IProductLookup
{
    public string Attribution => "Data: Fake (test)";

    public LookupResult Result { get; set; } = LookupResult.NotFound();

    public List<string> Asked { get; } = [];

    public Task<LookupResult> FindAsync(string barcode, CancellationToken cancellationToken = default)
    {
        Asked.Add(barcode);
        return Task.FromResult(Result);
    }
}
