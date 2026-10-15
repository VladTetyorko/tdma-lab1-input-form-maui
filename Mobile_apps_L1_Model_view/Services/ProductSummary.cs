namespace Mobile_apps_L1_Model_view.Services;

// One row of the product list: a product with its computed quantity on hand and its
// characteristic values (characteristic id -> stored text). Also the object that travels
// between pages as a navigation parameter.
public sealed record ProductSummary(Guid Id, string Name, string Barcode, int Quantity)
{
    private static readonly IReadOnlyDictionary<Guid, string> NoValues = new Dictionary<Guid, string>();

    public IReadOnlyDictionary<Guid, string> Values { get; init; } = NoValues;

    // At least one In/Out has been recorded. Such a product is kept for good: its history must stay complete
    public bool HasOperations { get; init; }

    public bool CanDelete => !HasOperations;
}
