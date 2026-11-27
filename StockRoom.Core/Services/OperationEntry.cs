using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// One operation with the names a person needs. Counterparty is personal data (decrypted): shown, never logged. Note is
// the document's note, or the reason of a single operation
public sealed record OperationEntry(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string Barcode,
    OperationDirection Direction,
    int Quantity,
    Guid UserId,
    string UserName,
    DateTime At)
{
    public string? DocumentNumber { get; init; }

    public string? Counterparty { get; init; }

    public string? Note { get; init; }

    public GeoPoint? Location { get; init; }

    // +12 for goods coming in, -12 for goods going out
    public int Effect => Direction == OperationDirection.In ? Quantity : -Quantity;
}
