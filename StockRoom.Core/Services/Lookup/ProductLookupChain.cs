using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services.Lookup;

// Asks the databases one after another until one knows the code (one request each, gentle on their rate limits).
// A source that fails never stops the others. Hits are remembered while the app runs, not on disk
public sealed class ProductLookupChain(IEnumerable<ILookupSource> sources, ILogger<ProductLookupChain> logger)
    : IProductLookup
{
    private readonly IReadOnlyList<ILookupSource> _sources = [.. sources];
    private readonly ConcurrentDictionary<string, LookupResult> _found = new();

    public async Task<LookupResult> FindAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (_found.TryGetValue(barcode, out var known))
        {
            return known;
        }

        var misses = new List<LookupStatus>();
        foreach (var source in _sources.Where(source => source.Covers(barcode)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await AskAsync(source, barcode, cancellationToken);
            if (result.IsFound)
            {
                logger.LogInformation("Barcode {Barcode} found in {Source}", barcode, source.Name);
                return _found[barcode] = result;
            }

            misses.Add(result.Status);
        }

        return misses.All(status => status == LookupStatus.NotFound) ? LookupResult.NotFound()
            : misses.All(status => status == LookupStatus.Offline) ? LookupResult.Offline()
            : LookupResult.Failed();
    }

    private async Task<LookupResult> AskAsync(ILookupSource source, string barcode, CancellationToken cancellationToken)
    {
        try
        {
            return await source.FindAsync(barcode, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Lookup source {Source} failed for {Barcode}", source.Name, barcode);
            return LookupResult.Failed();
        }
    }
}
