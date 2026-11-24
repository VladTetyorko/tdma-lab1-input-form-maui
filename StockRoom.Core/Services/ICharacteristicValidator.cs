using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Each method returns an error message for the user, or null when the input is valid
public interface ICharacteristicValidator
{
    // Required and not too long: cheap, safe to call on every keystroke
    string? ValidateName(string? name);

    // Name rules plus uniqueness among existing characteristics, ignoring case
    Task<string?> ValidateNameAsync(string? name);

    // Empty means "no value" and is valid. A Number must parse with the invariant culture
    string? ValidateValue(CharacteristicValueType type, string? value);
}
