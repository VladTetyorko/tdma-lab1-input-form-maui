using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

public partial class CharacteristicsViewModel(
    ICharacteristicService characteristics,
    ICharacteristicValidator validator,
    ICurrentUserService currentUser,
    INavigationService navigation) : ObservableObject, IPageLifecycleAware
{
    private bool _changed;

    public ObservableCollection<Characteristic> Items { get; } = [];

    public IReadOnlyList<CharacteristicValueType> ValueTypes { get; } = Enum.GetValues<CharacteristicValueType>();

    [ObservableProperty]
    public partial bool CanManage { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    public partial string NewName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial CharacteristicValueType NewValueType { get; set; }

    // Empty string = no error (keeps XAML bindings simple)
    [ObservableProperty]
    public partial string NameError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    partial void OnNewNameChanged(string value) => NameError = validator.ValidateName(value) ?? string.Empty;

    public void OnAppearing() => LoadCommand.Execute(null);

    [RelayCommand]
    private async Task LoadAsync()
    {
        var user = currentUser.CurrentUser;
        CanManage = user is not null && RolePermissions.CanManageProducts(user.Role);

        Items.Clear();
        foreach (var characteristic in await characteristics.GetAllAsync())
        {
            Items.Add(characteristic);
        }
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        // Uniqueness needs the repository, so it is checked on Add, not on every keystroke
        var error = await validator.ValidateNameAsync(NewName);
        if (error is not null)
        {
            NameError = error;
            return;
        }

        var result = await characteristics.AddAsync(NewName, NewValueType);
        if (!result.IsSuccess)
        {
            StatusMessage = result.Error!;
            return;
        }

        Items.Add(result.Value!);
        _changed = true;
        StatusMessage = string.Empty;
        ResetForm();
    }

    // Clearing the name fires live validation ("required"); an untouched form should show no error
    private void ResetForm()
    {
        NewName = string.Empty;
        NameError = string.Empty;
    }

    // Tells the list to rebuild its sort options only when something was added
    [RelayCommand]
    private Task GoBackAsync() =>
        _changed
            ? navigation.GoToAsync(Routes.Back,
                new Dictionary<string, object> { [NavigationKeys.CharacteristicsChanged] = true })
            : navigation.GoToAsync(Routes.Back);

    private bool CanAdd() => CanManage && validator.ValidateName(NewName) is null;

    partial void OnCanManageChanged(bool value) => AddCommand.NotifyCanExecuteChanged();
}
