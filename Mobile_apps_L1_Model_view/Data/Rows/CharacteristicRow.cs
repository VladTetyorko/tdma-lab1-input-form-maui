using SQLite;

namespace Mobile_apps_L1_Model_view.Data.Rows;

[Table("Characteristics")]
public sealed class CharacteristicRow : OwnedRow
{
    public string Name { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
}
