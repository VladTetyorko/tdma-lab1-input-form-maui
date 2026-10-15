using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class LoginViewModelTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly CurrentUserService _currentUser = new();
    private readonly FakeNavigationService _navigation = new();
    private readonly LoginViewModel _viewModel;

    public LoginViewModelTests()
    {
        _viewModel = new LoginViewModel(_users, _currentUser, _navigation);
    }

    [Fact]
    public async Task Load_SignsOutAndFillsUsers()
    {
        await AddThreeUsersAsync();
        _currentUser.SignIn(new User { FullName = "Ivan Shevchenko", Role = Role.Driver });

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Null(_currentUser.CurrentUser);
        Assert.Equal(3, _viewModel.Users.Count);
    }

    [Fact]
    public async Task Load_CalledTwice_DoesNotDuplicateUsers()
    {
        await AddThreeUsersAsync();

        await _viewModel.LoadCommand.ExecuteAsync(null);
        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(3, _viewModel.Users.Count);
    }

    [Fact]
    public async Task Load_ClearsPreviousSelection()
    {
        await AddThreeUsersAsync();
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.SelectedUser = _viewModel.Users[0];

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Null(_viewModel.SelectedUser);
    }

    [Fact]
    public async Task SignIn_IsDisabledWithoutSelection()
    {
        await AddThreeUsersAsync();
        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(_viewModel.SignInCommand.CanExecute(null));
    }

    [Fact]
    public async Task SignIn_IsEnabledWhenUserIsSelected()
    {
        await AddThreeUsersAsync();
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.SelectedUser = _viewModel.Users[0];

        Assert.True(_viewModel.SignInCommand.CanExecute(null));
    }

    [Fact]
    public async Task SignIn_SetsCurrentUserAndNavigatesToProducts()
    {
        await AddThreeUsersAsync();
        await _viewModel.LoadCommand.ExecuteAsync(null);
        var storekeeper = _viewModel.Users.Single(u => u.Role == Role.Storekeeper);
        _viewModel.SelectedUser = storekeeper;

        await _viewModel.SignInCommand.ExecuteAsync(null);

        Assert.Same(storekeeper, _currentUser.CurrentUser);
        Assert.Equal(Routes.Absolute(Routes.Products), Assert.Single(_navigation.RequestedRoutes));
    }

    private async Task AddThreeUsersAsync()
    {
        await _users.AddAsync(new User { FullName = "Olena Kovalenko", Role = Role.Manager });
        await _users.AddAsync(new User { FullName = "Petro Bondarenko", Role = Role.Storekeeper });
        await _users.AddAsync(new User { FullName = "Ivan Shevchenko", Role = Role.Driver });
    }
}
