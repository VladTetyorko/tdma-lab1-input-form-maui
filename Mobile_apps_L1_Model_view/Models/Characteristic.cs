namespace Mobile_apps_L1_Model_view.Models;

public class Characteristic : OwnedEntity
{
    public string Name { get; set; } = string.Empty;
    public CharacteristicValueType ValueType { get; init; }
}
