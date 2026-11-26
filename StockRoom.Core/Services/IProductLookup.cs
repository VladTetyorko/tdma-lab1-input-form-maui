namespace Mobile_apps_L1_Model_view.Services;

// Looks a barcode up in open product and book databases (online only, never throws for network problems).
// A found result names its source and the attribution that must be shown with the data
public interface IProductLookup
{
    Task<LookupResult> FindAsync(string barcode, CancellationToken cancellationToken = default);
}
