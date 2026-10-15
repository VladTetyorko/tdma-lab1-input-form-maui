namespace Mobile_apps_L1_Model_view.Models;

public class ProductCharacteristic : OwnedEntity
{
    public Guid ProductId { get; init; }
    public Guid CharacteristicId { get; init; }
    public string Value { get; set; } = string.Empty;
}
