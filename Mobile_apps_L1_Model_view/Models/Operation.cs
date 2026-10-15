namespace Mobile_apps_L1_Model_view.Models;

// Immutable: a mistake is fixed by a new operation in the opposite direction
public class Operation : OwnedEntity
{
    public Guid ProductId { get; init; }
    public OperationDirection Direction { get; init; }
    public int Quantity { get; init; }
    public string? Counterparty { get; init; }
    public string? Reason { get; init; }
}
