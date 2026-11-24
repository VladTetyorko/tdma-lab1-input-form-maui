using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// The server side of synchronisation. The app ships a local stub (StubSyncApi); a real implementation
// would send the item to a REST endpoint with HttpClient. Pushing the same item twice must be harmless.
public interface ISyncApi
{
    // Throws SyncApiException on failure
    Task<SyncPushOutcome> PushAsync(SyncQueueItem item, CancellationToken cancellationToken = default);
}
