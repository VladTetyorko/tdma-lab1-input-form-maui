using SQLite;

namespace Mobile_apps_L1_Model_view.Data.Rows;

[Table("Operations")]
public sealed class OperationRow : OwnedRow
{
    public string ProductId { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public int Quantity { get; set; }

    // Encrypted (base64) when the operation has a counterparty
    public string? Counterparty { get; set; }
    public string? Reason { get; set; }
}
