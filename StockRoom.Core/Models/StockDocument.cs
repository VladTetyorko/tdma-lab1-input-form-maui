namespace Mobile_apps_L1_Model_view.Models;

// One receipt (In) or one issue (Out) of several products. Immutable like its operations:
// a mistake is fixed by a new document or operation in the opposite direction
public class StockDocument : OwnedEntity
{
    // Human-readable and unique: "R-20261124-0001" (receipt) or "I-..." (issue)
    public string Number { get; init; } = string.Empty;

    public OperationDirection Direction { get; init; }

    // Supplier or recipient: personal data, stored encrypted and never logged
    public string? Counterparty { get; init; }

    // Waybill number, order number, ...
    public string? Note { get; init; }
}
