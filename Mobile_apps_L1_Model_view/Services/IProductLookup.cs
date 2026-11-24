namespace Mobile_apps_L1_Model_view.Services;

// Looks a barcode up in an open product database (online only, never throws for network problems)
public interface IProductLookup
{
    // Text that must be shown wherever looked-up data is displayed (licence attribution)
    string Attribution { get; }

    Task<LookupResult> FindAsync(string barcode, CancellationToken cancellationToken = default);
}
