namespace Mobile_apps_L1_Model_view.Services.Lookup;

// For a screen that shows one code at a time: a new lookup, or leaving, cancels the one still running
public sealed class LatestLookup(IProductLookup lookup)
{
    private CancellationTokenSource? _running;

    // Null when the answer is no longer wanted
    public async Task<LookupResult?> FindAsync(string barcode)
    {
        Cancel();
        var current = _running = new CancellationTokenSource();
        try
        {
            var result = await lookup.FindAsync(barcode, current.Token);
            return current.IsCancellationRequested ? null : result;
        }
        catch (OperationCanceledException) when (current.IsCancellationRequested)
        {
            return null;
        }
    }

    public void Cancel()
    {
        _running?.Cancel();
        _running = null;
    }
}
