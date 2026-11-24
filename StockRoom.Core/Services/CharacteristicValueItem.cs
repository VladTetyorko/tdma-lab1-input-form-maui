using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// A characteristic as seen on one product; Value is empty when the product has none
public sealed record CharacteristicValueItem(
    Guid CharacteristicId, string Name, CharacteristicValueType ValueType, string Value);
