using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// One entry of the "sort by" picker; ToString is what the picker displays
public sealed record SortOption(
    string Title, SortKind Kind, Guid CharacteristicId = default, CharacteristicValueType ValueType = default)
{
    public static SortOption ByName { get; } = new("Name", SortKind.Name);

    public static SortOption ByQuantity { get; } = new("Quantity", SortKind.Quantity);

    public static SortOption ByCharacteristic(Characteristic characteristic) =>
        new(characteristic.Name, SortKind.Characteristic, characteristic.Id, characteristic.ValueType);

    // Name, Quantity, then one option per characteristic, in the given order
    public static IReadOnlyList<SortOption> All(IEnumerable<Characteristic> characteristics) =>
        [ByName, ByQuantity, .. characteristics.Select(ByCharacteristic)];

    public override string ToString() => Title;
}
