using Mobile_apps_L1_Model_view.Data.Rows;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

public sealed class SqliteSyncQueue(IDatabase database) : ISyncQueue
{
    public async Task<IReadOnlyList<SyncQueueItem>> GetPendingAsync()
    {
        var connection = await database.GetConnectionAsync();
        var rows = await connection.QueryAsync<SyncQueueRow>("SELECT * FROM SyncQueue ORDER BY rowid");
        return rows.Select(ToItem).ToList();
    }

    public async Task<int> CountAsync()
    {
        var connection = await database.GetConnectionAsync();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SyncQueue");
    }

    public async Task RemoveAsync(Guid id)
    {
        var connection = await database.GetConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM SyncQueue WHERE Id = ?", RowFormat.Text(id));
    }

    public async Task MarkAttemptedAsync(Guid id, DateTime attemptedAt)
    {
        var connection = await database.GetConnectionAsync();
        await connection.ExecuteAsync(
            "UPDATE SyncQueue SET UpdatedAt = ? WHERE Id = ?", RowFormat.Text(attemptedAt), RowFormat.Text(id));
    }

    private static SyncQueueItem ToItem(SyncQueueRow row) => new()
    {
        Id = RowFormat.ToGuid(row.Id),
        EntityType = row.EntityType,
        EntityId = RowFormat.ToGuid(row.EntityId),
        Operation = RowFormat.ToEnum<SyncOperation>(row.Operation),
        PayloadJson = row.PayloadJson,
        CreatedAt = RowFormat.ToTime(row.CreatedAt),
        UpdatedAt = RowFormat.ToTime(row.UpdatedAt)
    };
}
