using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// One line of a product's history: who moved how many units, when (UTC), and why
public sealed record OperationHistoryItem(
    DateTime At, OperationDirection Direction, int Quantity, string UserName, string? Counterparty, string? Reason)
{
    // "Acme Ltd · Initial delivery"; empty when neither was given
    public string Details =>
        string.Join(" · ", new[] { Counterparty, Reason }.Where(text => !string.IsNullOrWhiteSpace(text)));
}
