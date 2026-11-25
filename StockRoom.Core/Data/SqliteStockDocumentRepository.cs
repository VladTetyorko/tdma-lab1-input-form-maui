using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data.Rows;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Data;

public sealed class SqliteStockDocumentRepository(
    IDatabase database,
    SyncJournal journal,
    IFieldEncryptor encryptor,
    ILogger<SqliteStockDocumentRepository> logger)
    : SqliteRepository<StockDocument, StockDocumentRow>(database, journal, logger), IStockDocumentRepository
{
    private readonly IDatabase _database = database;
    private readonly SyncJournal _journal = journal;

    public Task<StockDocument?> GetByIdAsync(Guid id) => FindAsync("WHERE Id = ?", RowFormat.Text(id));

    public async Task<IReadOnlyList<StockDocument>> GetRecentAsync(int count)
    {
        var connection = await _database.GetConnectionAsync();
        var rows = await connection.QueryAsync<StockDocumentRow>(
            "SELECT * FROM StockDocuments ORDER BY CreatedAt DESC, rowid DESC LIMIT ?", count);

        var documents = new List<StockDocument>(rows.Count);
        foreach (var row in rows)
        {
            documents.Add(await ToEntityAsync(row));
        }

        return documents;
    }

    public async Task<IReadOnlyList<string>> GetRecentCounterpartiesAsync(
        OperationDirection direction, int count, Guid? createdBy = null)
    {
        var connection = await _database.GetConnectionAsync();
        var rows = await connection.QueryAsync<StockDocumentRow>(
            "SELECT * FROM StockDocuments WHERE Direction = ? AND Counterparty IS NOT NULL AND (? IS NULL OR CreatedByUserId = ?) "
            + "ORDER BY CreatedAt DESC, rowid DESC LIMIT 100",
            RowFormat.Text(direction), createdBy is { } user ? RowFormat.Text(user) : null,
            createdBy is { } same ? RowFormat.Text(same) : null);

        var names = new List<string>();
        foreach (var row in rows)
        {
            var name = await DecryptAsync(row);
            if (name != SqliteOperationRepository.UnreadableCounterparty && !names.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(name);
                if (names.Count == count)
                {
                    break;
                }
            }
        }

        return names;
    }

    public async Task<int> CountByNumberPrefixAsync(string prefix)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM StockDocuments WHERE substr(Number, 1, ?) = ?", prefix.Length, prefix);
    }

    public Task AddAsync(StockDocument document, IReadOnlyList<Operation> operations) => AddAsync([(document, operations)]);

    public async Task AddAsync(IReadOnlyList<(StockDocument Document, IReadOnlyList<Operation> Operations)> batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        foreach (var (document, operations) in batch)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(operations);
            if (operations.Count == 0 || operations.Any(o => o.DocumentId != document.Id || o.Counterparty is not null))
            {
                throw new ArgumentException("A document needs operations that point to it and carry no counterparty.", nameof(batch));
            }
        }

        // Encryption is async, so every row is prepared before the (synchronous) transaction starts
        var prepared = new List<(StockDocument Document, StockDocumentRow Row, List<(OperationRow Row, DateTime ChangedAt)> Lines)>();
        foreach (var (document, operations) in batch)
        {
            prepared.Add((document, await ToRowAsync(document),
                [.. operations.Select(o => (OperationRowMapper.ToRow(o, null), o.UpdatedAt))]));
        }

        await InTransactionAsync(transaction =>
        {
            foreach (var (document, documentRow, rows) in prepared)
            {
                // The document goes first, so a server applying the queue in order never sees a dangling DocumentId
                transaction.Insert(documentRow);
                _journal.Record(transaction, documentRow, SyncOperation.Insert, document.UpdatedAt);
                foreach (var (row, changedAt) in rows)
                {
                    transaction.Insert(row);
                    _journal.Record(transaction, row, SyncOperation.Insert, changedAt);
                }
            }
        });
    }

    protected override async ValueTask<StockDocumentRow> ToRowAsync(StockDocument document) => new()
    {
        Id = RowFormat.Text(document.Id),
        Number = document.Number,
        Direction = RowFormat.Text(document.Direction),
        Counterparty = document.Counterparty is null ? null : await encryptor.EncryptAsync(document.Counterparty),
        Note = document.Note,
        CreatedByUserId = RowFormat.Text(document.CreatedByUserId),
        CreatedAt = RowFormat.Text(document.CreatedAt),
        UpdatedAt = RowFormat.Text(document.UpdatedAt)
    };

    protected override async ValueTask<StockDocument> ToEntityAsync(StockDocumentRow row) => new()
    {
        Id = RowFormat.ToGuid(row.Id),
        Number = row.Number,
        Direction = RowFormat.ToEnum<OperationDirection>(row.Direction),
        Counterparty = row.Counterparty is null ? null : await DecryptAsync(row),
        Note = row.Note,
        CreatedByUserId = RowFormat.ToGuid(row.CreatedByUserId),
        CreatedAt = RowFormat.ToTime(row.CreatedAt),
        UpdatedAt = RowFormat.ToTime(row.UpdatedAt)
    };

    private async Task<string> DecryptAsync(StockDocumentRow row)
    {
        try
        {
            return await encryptor.DecryptAsync(row.Counterparty!);
        }
        catch (EncryptionException ex)
        {
            Logger.LogWarning(ex, "Counterparty of document {Document} cannot be decrypted", row.Id);
            return SqliteOperationRepository.UnreadableCounterparty;
        }
    }
}
