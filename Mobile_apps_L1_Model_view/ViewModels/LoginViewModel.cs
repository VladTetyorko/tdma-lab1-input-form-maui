using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

public partial class LoginViewModel(
    IUserRepository users,
    ICurrentUserService currentUser,
    INavigationService navigation) : ObservableObject, IPageLifecycleAware
{
    public ObservableCollection<User> Users { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    public partial User? SelectedUser { get; set; }

    public void OnAppearing() => LoadCommand.Execute(null);

    [RelayCommand]
    private async Task LoadAsync()
    {
        currentUser.SignOut();
        SelectedUser = null;
        Users.Clear();
        foreach (var user in await users.GetAllAsync())
        {
            Users.Add(user);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private async Task SignInAsync()
    {
        currentUser.SignIn(SelectedUser!);
        await navigation.GoToAsync(Routes.Absolute(Routes.Products));
    }

    private bool CanSignIn() => SelectedUser is not null;
}
