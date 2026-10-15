using System.Text.Json;
using Mobile_apps_L1_Model_view.Data.Rows;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using SQLite;

namespace Mobile_apps_L1_Model_view.Data;

// The one place that writes to SyncQueue. Repositories call it inside the same transaction as the
// change itself, so a change and its queue entry are saved together or not at all.
public sealed class SyncJournal(IClock clock)
{
    // changedAt: when the change was made (the row's UpdatedAt); null means now (used for deletes)
    public void Record(SQLiteConnection transaction, Row row, SyncOperation operation, DateTime? changedAt = null)
    {
        var at = RowFormat.Text(changedAt ?? clock.UtcNow);
        transaction.Insert(new SyncQueueRow
        {
            Id = RowFormat.Text(Guid.CreateVersion7()),
            EntityType = RowTables.NameOf(row.GetType()),
            EntityId = row.Id,
            Operation = RowFormat.Text(operation),
            PayloadJson = JsonSerializer.Serialize(row, row.GetType()),
            CreatedAt = at,
            UpdatedAt = at
        });
    }
}
