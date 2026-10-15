using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

// Read and clean-up side of the outbox. Items are added only by SyncJournal, inside the transaction of the change
public interface ISyncQueue
{
    // Oldest first: the order in which the changes were made
    Task<IReadOnlyList<SyncQueueItem>> GetPendingAsync();

    Task<int> CountAsync();

    // The server has the change
    Task RemoveAsync(Guid id);

    // The push failed: the item stays queued; UpdatedAt records the attempt
    Task MarkAttemptedAsync(Guid id, DateTime attemptedAt);
}
