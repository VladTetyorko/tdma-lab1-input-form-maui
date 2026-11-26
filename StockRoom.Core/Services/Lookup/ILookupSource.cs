namespace Mobile_apps_L1_Model_view.Services.Lookup;

// One open database. The chain asks the sources in their registration order
public interface ILookupSource
{
    string Name { get; }

    // False when the database cannot know this kind of code, so no request is made
    bool Covers(string barcode);

    // Never throws for network or data problems; a cancellation by the caller is passed on
    Task<LookupResult> FindAsync(string barcode, CancellationToken cancellationToken = default);
}
