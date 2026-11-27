using SQLite;

namespace Mobile_apps_L1_Model_view.Data.Rows;

[Table("StockDocuments")]
public sealed class StockDocumentRow : OwnedRow
{
    public string Number { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;

    // Encrypted (base64) when the document has a supplier or recipient
    public string? Counterparty { get; set; }
    public string? Note { get; set; }
}
