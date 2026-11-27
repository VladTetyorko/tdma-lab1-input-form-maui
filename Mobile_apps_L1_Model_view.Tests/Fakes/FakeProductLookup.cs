using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// The open databases as the test wants them: one answer for every code, optionally held back; remembers the codes asked
public sealed class FakeProductLookup : IProductLookup
{
    public string Source => "Fake Facts";

    public string Attribution => "Data: Fake (test)";

    public LookupResult Result { get; set; } = LookupResult.NotFound();

    // Set to keep the next answers back until it completes
    public TaskCompletionSource? Gate { get; set; }

    public List<string> Asked { get; } = [];

    public async Task<LookupResult> FindAsync(string barcode, CancellationToken cancellationToken = default)
    {
        Asked.Add(barcode);
        var result = Result.IsFound ? Result with { Source = Source, Attribution = Attribution } : Result;
        if (Gate is { } gate)
        {
            await gate.Task.WaitAsync(cancellationToken);
        }

        return result;
    }
}
