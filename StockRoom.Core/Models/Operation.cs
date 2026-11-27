namespace Mobile_apps_L1_Model_view.Models;

// Immutable: a mistake is fixed by a new operation in the opposite direction
public class Operation : OwnedEntity
{
    public Guid ProductId { get; init; }

    // The batch document this operation belongs to (null for a single In/Out). Counterparty and Reason of such
    // an operation stay empty: those facts belong to the document
    public Guid? DocumentId { get; init; }

    public OperationDirection Direction { get; init; }
    public int Quantity { get; init; }
    public string? Counterparty { get; init; }
    public string? Reason { get; init; }

    // Where it was recorded (optional, e.g. a driver's drop-off point)
    public GeoPoint? Location { get; init; }
}
