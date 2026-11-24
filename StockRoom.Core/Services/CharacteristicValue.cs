using System.Globalization;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// How characteristic values are read and stored. Always the invariant culture, so "1.5" means
// the same on every device; the validator, the service and the sorter all use this one class.
public static class CharacteristicValue
{
    public static bool TryParseNumber(string? text, out double number) =>
        double.TryParse(text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
        && double.IsFinite(number);

    // Text is trimmed; a Number is rewritten in canonical form ("01.50" -> "1.5").
    // The value must already be valid (see ICharacteristicValidator.ValidateValue)
    public static string Normalize(CharacteristicValueType type, string? value)
    {
        var text = value?.Trim() ?? string.Empty;
        return type == CharacteristicValueType.Number && TryParseNumber(text, out var number)
            ? number.ToString(CultureInfo.InvariantCulture)
            : text;
    }
}
