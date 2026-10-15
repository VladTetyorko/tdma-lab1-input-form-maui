using SQLite;

namespace Mobile_apps_L1_Model_view.Data.Rows;

[Table("ProductCharacteristics")]
public sealed class ProductCharacteristicRow : OwnedRow
{
    public string ProductId { get; set; } = string.Empty;
    public string CharacteristicId { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
