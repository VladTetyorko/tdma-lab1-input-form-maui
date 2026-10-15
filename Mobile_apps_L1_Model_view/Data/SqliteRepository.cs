using Mobile_apps_L1_Model_view.Data.Rows;
using Mobile_apps_L1_Model_view.Models;
using SQLite;

namespace Mobile_apps_L1_Model_view.Data;

// Shared SQLite plumbing for the repositories: queries, and writes that also record the change in
// SyncQueue (through SyncJournal, in the same transaction). Subclasses only convert entity <-> row.
public abstract class SqliteRepository<TEntity, TRow>(IDatabase database, SyncJournal journal)
    where TEntity : Entity
    where TRow : Row, new()
{
    private static readonly string Table = RowTables.NameOf<TRow>();

    protected abstract ValueTask<TRow> ToRowAsync(TEntity entity);

    protected abstract ValueTask<TEntity> ToEntityAsync(TRow row);

    // where: an optional "WHERE ..." clause with ? placeholders; rows come back in insertion order
    protected async Task<IReadOnlyList<TEntity>> QueryAsync(string where = "", params object[] args)
    {
        var connection = await database.GetConnectionAsync();
        var rows = await connection.QueryAsync<TRow>($"SELECT * FROM {Table} {where} ORDER BY rowid", args);

        var entities = new List<TEntity>(rows.Count);
        foreach (var row in rows)
        {
            entities.Add(await ToEntityAsync(row));
        }

        return entities;
    }

    protected async Task<TEntity?> FindAsync(string where, params object[] args)
    {
        var connection = await database.GetConnectionAsync();
        var rows = await connection.QueryAsync<TRow>($"SELECT * FROM {Table} {where} LIMIT 1", args);
        return rows.Count == 0 ? null : await ToEntityAsync(rows[0]);
    }

    protected async Task InsertAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var row = await ToRowAsync(entity);
        await InTransactionAsync(transaction =>
        {
            transaction.Insert(row);
            journal.Record(transaction, row, SyncOperation.Insert, entity.UpdatedAt);
        });
    }

    protected async Task UpdateAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var row = await ToRowAsync(entity);
        await InTransactionAsync(transaction =>
        {
            if (transaction.Update(row) == 0)
            {
                throw new InvalidOperationException($"{typeof(TEntity).Name} {entity.Id} does not exist.");
            }

            journal.Record(transaction, row, SyncOperation.Update, entity.UpdatedAt);
        });
    }

    // Removing a row that is already gone is not an error
    protected Task DeleteAsync(Guid id) =>
        InTransactionAsync(transaction =>
        {
            if (transaction.Find<TRow>(RowFormat.Text(id)) is { } row)
            {
                DeleteRow(transaction, row);
            }
        });

    protected async Task InTransactionAsync(Action<SQLiteConnection> work)
    {
        var connection = await database.GetConnectionAsync();
        await connection.RunInTransactionAsync(work);
    }

    // Deletes any row (also of another table) and queues the deletion
    protected void DeleteRow<TOther>(SQLiteConnection transaction, TOther row) where TOther : Row
    {
        transaction.Delete(row);
        journal.Record(transaction, row, SyncOperation.Delete);
    }
}
