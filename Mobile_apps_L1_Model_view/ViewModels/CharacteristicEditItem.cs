using CommunityToolkit.Mvvm.ComponentModel;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// One editable row on the detail page: a characteristic and this product's value for it
public partial class CharacteristicEditItem : ObservableObject
{
    private readonly ICharacteristicValidator _validator;

    public CharacteristicEditItem(CharacteristicValueItem source, ICharacteristicValidator validator, bool canEdit)
    {
        _validator = validator;
        CharacteristicId = source.CharacteristicId;
        Name = source.Name;
        ValueType = source.ValueType;
        IsReadOnly = !canEdit;
        Value = source.Value;
    }

    public Guid CharacteristicId { get; }

    public string Name { get; }

    public CharacteristicValueType ValueType { get; }

    public bool IsReadOnly { get; }

    public string Placeholder => ValueType == CharacteristicValueType.Number ? "number" : "text";

    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;

    // Empty string = no error (keeps XAML bindings simple)
    [ObservableProperty]
    public partial string Error { get; set; } = string.Empty;

    partial void OnValueChanged(string value) => Error = _validator.ValidateValue(ValueType, value) ?? string.Empty;
}
