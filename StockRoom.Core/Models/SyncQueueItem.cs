namespace Mobile_apps_L1_Model_view.Models;

// One local write the server has not received yet (offline-first outbox). Infrastructure, not domain data.
// CreatedAt is the moment of the change: it decides "last write wins" on the server.
// UpdatedAt changes only when a push attempt fails.
public class SyncQueueItem : Entity
{
    // Table name of the changed entity, e.g. "Products"
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public SyncOperation Operation { get; init; }

    // Snapshot of the written row as stored (Operations keep Counterparty encrypted); for Delete, the removed row
    public string PayloadJson { get; init; } = string.Empty;
}
