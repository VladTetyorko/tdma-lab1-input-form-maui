using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// What the stub server remembers about one entity: the latest accepted change
public sealed record StubServerRecord(
    string EntityType, Guid EntityId, SyncOperation Operation, string PayloadJson, DateTime ChangedAt)
{
    // A delete is kept as a tombstone so an older update that arrives late cannot bring the entity back
    public bool IsDeleted => Operation == SyncOperation.Delete;
}
