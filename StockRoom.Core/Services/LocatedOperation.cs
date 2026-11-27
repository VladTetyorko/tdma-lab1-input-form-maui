using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// An operation that was recorded with a location: one pin of the map page (read side, built by IProductCatalog)
public sealed record LocatedOperation(
    Guid OperationId,
    string ProductName,
    string Barcode,
    OperationDirection Direction,
    int Quantity,
    DateTime At,
    string UserName,
    GeoPoint Location)
{
    // "Out 3 x Drill"
    public string Label => $"{Direction} {Quantity} x {ProductName}";

    // "Petro Shevchenko, 2026-01-15 09:30 UTC, 50.45010, 30.52340"
    public string Details => $"{UserName}, {At:yyyy-MM-dd HH:mm} UTC, {Location}";
}
