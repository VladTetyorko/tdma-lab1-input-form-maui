using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data.Rows;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Data;

// Counterparty is personal data: it is encrypted on its way into the row and decrypted on its way out.
// Every other column stays plain, so an exported database is readable.
public sealed class SqliteOperationRepository(
    IDatabase database,
    SyncJournal journal,
    IFieldEncryptor encryptor,
    ILogger<SqliteOperationRepository> logger)
    : SqliteRepository<Operation, OperationRow>(database, journal), IOperationRepository
{
    // Shown instead of the counterparty when it cannot be decrypted (wrong or missing key), so one
    // unreadable value does not break the whole history
    public const string UnreadableCounterparty = "(encrypted - cannot be read)";

    private readonly IDatabase _database = database;

    public Task<IReadOnlyList<Operation>> GetAllAsync() => QueryAsync();

    public Task<IReadOnlyList<Operation>> GetByProductAsync(Guid productId) =>
        QueryAsync("WHERE ProductId = ?", RowFormat.Text(productId));

    public async Task<bool> AnyForProductAsync(Guid productId)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT EXISTS (SELECT 1 FROM Operations WHERE ProductId = ?)", RowFormat.Text(productId)) == 1;
    }

    public Task AddAsync(Operation operation) => InsertAsync(operation);

    protected override async ValueTask<OperationRow> ToRowAsync(Operation operation) => new()
    {
        Id = RowFormat.Text(operation.Id),
        ProductId = RowFormat.Text(operation.ProductId),
        Direction = RowFormat.Text(operation.Direction),
        Quantity = operation.Quantity,
        Counterparty = operation.Counterparty is null ? null : await encryptor.EncryptAsync(operation.Counterparty),
        Reason = operation.Reason,
        CreatedByUserId = RowFormat.Text(operation.CreatedByUserId),
        CreatedAt = RowFormat.Text(operation.CreatedAt),
        UpdatedAt = RowFormat.Text(operation.UpdatedAt)
    };

    protected override async ValueTask<Operation> ToEntityAsync(OperationRow row) => new()
    {
        Id = RowFormat.ToGuid(row.Id),
        ProductId = RowFormat.ToGuid(row.ProductId),
        Direction = RowFormat.ToEnum<OperationDirection>(row.Direction),
        Quantity = row.Quantity,
        Counterparty = row.Counterparty is null ? null : await DecryptAsync(row),
        Reason = row.Reason,
        CreatedByUserId = RowFormat.ToGuid(row.CreatedByUserId),
        CreatedAt = RowFormat.ToTime(row.CreatedAt),
        UpdatedAt = RowFormat.ToTime(row.UpdatedAt)
    };

    private async Task<string> DecryptAsync(OperationRow row)
    {
        try
        {
            return await encryptor.DecryptAsync(row.Counterparty!);
        }
        catch (EncryptionException ex)
        {
            logger.LogWarning(ex, "Counterparty of operation {Operation} cannot be decrypted", row.Id);
            return UnreadableCounterparty;
        }
    }
}
