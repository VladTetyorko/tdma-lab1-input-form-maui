using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// One line of a product's history: who moved how many units, when (UTC), and why
public sealed record OperationHistoryItem(
    DateTime At, OperationDirection Direction, int Quantity, string UserName, string? Counterparty, string? Reason,
    GeoPoint? Location = null)
{
    // The history line shows a marker and an "Open in maps" button only for operations recorded with a location
    public bool HasLocation => Location is not null;

    public string LocationText => Location is null ? string.Empty : $"Location {Location}";

    // "Acme Ltd · Initial delivery"; empty when neither was given
    public string Details =>
        string.Join(" · ", new[] { Counterparty, Reason }.Where(text => !string.IsNullOrWhiteSpace(text)));
}
