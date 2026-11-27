using Mobile_apps_L1_Model_view.Data.Rows;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

// Operation -> row, shared by the repository of single operations and the one of documents.
// The counterparty is passed in already encrypted: this class never sees a key
internal static class OperationRowMapper
{
    public static OperationRow ToRow(Operation operation, string? encryptedCounterparty) => new()
    {
        Id = RowFormat.Text(operation.Id),
        ProductId = RowFormat.Text(operation.ProductId),
        DocumentId = operation.DocumentId is { } document ? RowFormat.Text(document) : null,
        Direction = RowFormat.Text(operation.Direction),
        Quantity = operation.Quantity,
        Counterparty = encryptedCounterparty,
        Reason = operation.Reason,
        Latitude = operation.Location?.Latitude,
        Longitude = operation.Location?.Longitude,
        CreatedByUserId = RowFormat.Text(operation.CreatedByUserId),
        CreatedAt = RowFormat.Text(operation.CreatedAt),
        UpdatedAt = RowFormat.Text(operation.UpdatedAt)
    };
}
