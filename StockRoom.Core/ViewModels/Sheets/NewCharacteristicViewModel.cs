using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels.Sheets;

// "New characteristic": a name and Text or Number. The service refuses a role that may not add one
public sealed partial class NewCharacteristicViewModel : SheetViewModel
{
    private readonly ICharacteristicService _characteristics;
    private readonly ICharacteristicValidator _validator;

    public NewCharacteristicViewModel(ICharacteristicService characteristics, ICharacteristicValidator validator)
    {
        _characteristics = characteristics;
        _validator = validator;
        Title = "New characteristic";
        PrimaryText = "Add characteristic";
    }

    // The characteristic that was added, or null
    public Characteristic? Added { get; private set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsText))]
    [NotifyPropertyChangedFor(nameof(IsNumber))]
    public partial CharacteristicValueType ValueType { get; set; }

    public bool IsText => ValueType == CharacteristicValueType.Text;

    public bool IsNumber => ValueType == CharacteristicValueType.Number;

    protected override bool CanAccept => _validator.ValidateName(Name) is null;

    partial void OnNameChanged(string value)
    {
        // An untouched field shows no error
        Hint = value.Length == 0 ? string.Empty : _validator.ValidateName(value) ?? string.Empty;
        RefreshPrimary();
    }

    [RelayCommand]
    private void SelectType(CharacteristicValueType type) => ValueType = type;

    protected override async Task<bool> OnPrimaryAsync()
    {
        // Uniqueness needs the repository, so it is checked on Add, not on every keystroke
        if (await _validator.ValidateNameAsync(Name) is { } error)
        {
            Hint = error;
            return false;
        }

        var result = await _characteristics.AddAsync(Name, ValueType);
        if (!result.IsSuccess)
        {
            Hint = result.Error!;
            return false;
        }

        Added = result.Value;
        return true;
    }
}
