using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class CharacteristicValidator(ICharacteristicRepository characteristics) : ICharacteristicValidator
{
    public const int MaxNameLength = 50;
    public const int MaxValueLength = 100;

    public string? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        return name.Trim().Length > MaxNameLength
            ? $"Name must be at most {MaxNameLength} characters."
            : null;
    }

    public async Task<string?> ValidateNameAsync(string? name)
    {
        var error = ValidateName(name);
        if (error is not null)
        {
            return error;
        }

        var trimmed = name!.Trim();
        return (await characteristics.GetAllAsync()).Any(c =>
            string.Equals(c.Name, trimmed, StringComparison.OrdinalIgnoreCase))
            ? "A characteristic with this name already exists."
            : null;
    }

    public string? ValidateValue(CharacteristicValueType type, string? value)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (type == CharacteristicValueType.Number)
        {
            return CharacteristicValue.TryParseNumber(text, out _) ? null : "Enter a number, for example 12.5.";
        }

        return text.Length > MaxValueLength ? $"Value must be at most {MaxValueLength} characters." : null;
    }
}
