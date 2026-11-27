using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.ViewModels;

// The list of characteristics; a new one is typed in a sheet (the same one the new-product wizard offers)
public partial class CharacteristicsViewModel(
    ICharacteristicService characteristics,
    ICharacteristicValidator validator,
    ICurrentUserService currentUser,
    IPermissionGuard guard,
    INavigationService navigation,
    ISheetService sheets) : ObservableObject, IPageLifecycleAware
{
    private bool _changed;

    public ObservableCollection<Characteristic> Items { get; } = [];

    [ObservableProperty]
    public partial bool CanManage { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

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

    // Manager only (the service refuses it as well)
    [RelayCommand]
    private async Task NewAsync()
    {
        if (await guard.CheckAsync(RolePermissions.CanManageProducts, "add characteristics") is { } refusal)
        {
            StatusMessage = refusal;
            return;
        }

        StatusMessage = string.Empty;
        var sheet = new NewCharacteristicViewModel(characteristics, validator);
        await sheets.ShowAsync(sheet);
        if (sheet.Added is { } added)
        {
            Items.Add(added);
            _changed = true;
        }
    }

    // Tells the list to rebuild its sort options only when something was added
    [RelayCommand]
    private Task GoBackAsync() =>
        _changed
            ? navigation.GoToAsync(Routes.Back,
                new Dictionary<string, object> { [NavigationKeys.CharacteristicsChanged] = true })
            : navigation.GoToAsync(Routes.Back);
}
