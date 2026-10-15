using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class AppLifecycleServiceTests
{
    private readonly FakePreferencesStore _preferences = new();
    private readonly FakeThemeApplier _applier = new();
    private readonly InMemoryUserRepository _users = new();
    private readonly CurrentUserService _currentUser = new();
    private readonly FakeNavigationService _navigation = new();
    private readonly FakeSyncService _sync = new();
    private readonly AppStateService _state;
    private readonly AppLifecycleService _lifecycle;

    public AppLifecycleServiceTests()
    {
        _state = new AppStateService(_preferences);
        var theme = new ThemeService(_preferences, _applier);
        _lifecycle = new AppLifecycleService(_state, _currentUser, theme, _users, _navigation, _sync,
            NullLogger<AppLifecycleService>.Instance);
    }

    [Fact]
    public async Task RestoreAsync_SavedExistingUser_SignsInAgainAndReturnsProducts()
    {
        var user = new User { FullName = "Petro Bondarenko", Role = Role.Storekeeper };
        await _users.AddAsync(user);
        _state.SignedInUserId = user.Id;
        _state.Save();

        var route = await _lifecycle.RestoreAsync();

        Assert.Equal(Routes.Products, route);
        Assert.Equal(user.Id, _currentUser.CurrentUser?.Id);
    }

    [Fact]
    public async Task RestoreAsync_SavedRoute_IsOfferedForResumeAfterStart()
    {
        var user = new User { FullName = "Petro Bondarenko", Role = Role.Storekeeper };
        await _users.AddAsync(user);
        _state.SignedInUserId = user.Id;
        _state.LastRoute = Routes.Operation;
        _state.Save();

        await _lifecycle.RestoreAsync();

        Assert.Equal(Routes.Operation, _state.TakeResumeRoute());
    }

    [Fact]
    public async Task RestoreAsync_UnknownUser_ReturnsLoginAndClearsTheSavedState()
    {
        _state.SignedInUserId = Guid.NewGuid();
        _state.LastRoute = Routes.ProductDetail;
        _state.SetDraft(new ProductFormDraft("Saw", "4820001000016"));
        _state.Save();

        var route = await _lifecycle.RestoreAsync();

        Assert.Equal(Routes.Login, route);
        Assert.Null(_currentUser.CurrentUser);
        Assert.Empty(_preferences.Values);
        Assert.Null(_state.TakeResumeRoute());
    }

    [Fact]
    public async Task RestoreAsync_NothingSaved_ReturnsLogin()
    {
        var route = await _lifecycle.RestoreAsync();

        Assert.Equal(Routes.Login, route);
        Assert.Null(_currentUser.CurrentUser);
    }

    [Fact]
    public async Task RestoreAsync_LoadsTheSavedTheme()
    {
        _preferences.Values["theme"] = nameof(ThemeSetting.Dark);

        await _lifecycle.RestoreAsync();

        Assert.Equal(new[] { ThemeSetting.Dark }, _applier.Applied);
    }

    [Fact]
    public async Task RestoreAsync_NoThemeSaved_AppliesSystem()
    {
        await _lifecycle.RestoreAsync();

        Assert.Equal(new[] { ThemeSetting.System }, _applier.Applied);
    }

    [Fact]
    public void OnStopped_SavesTheSignedInUserAndTheCurrentRoute()
    {
        var user = new User { FullName = "Petro Bondarenko", Role = Role.Storekeeper };
        _currentUser.SignIn(user);
        _navigation.CurrentRoute = Routes.ProductDetail;

        _lifecycle.OnStopped();

        var restored = new AppStateService(_preferences);
        restored.Restore();
        Assert.Equal(user.Id, restored.SignedInUserId);
        Assert.Equal(Routes.ProductDetail, restored.LastRoute);
    }

    [Fact]
    public void OnDestroying_SavesTheSignedInUserAndTheCurrentRoute()
    {
        var user = new User { FullName = "Olena Kovalenko", Role = Role.Manager };
        _currentUser.SignIn(user);
        _navigation.CurrentRoute = Routes.Scanner;

        _lifecycle.OnDestroying();

        var restored = new AppStateService(_preferences);
        restored.Restore();
        Assert.Equal(user.Id, restored.SignedInUserId);
        Assert.Equal(Routes.Scanner, restored.LastRoute);
    }

    [Fact]
    public void OnStopped_NobodySignedIn_SavesNoUser()
    {
        _navigation.CurrentRoute = Routes.Login;

        _lifecycle.OnStopped();

        Assert.False(_preferences.Values.ContainsKey("state.user"));
        Assert.Equal(Routes.Login, _preferences.Values["state.route"]);
    }

    [Fact]
    public void OtherWindowEvents_DoNotChangeStateOrSaveAnything()
    {
        _currentUser.SignIn(new User { FullName = "Ivan Shevchenko", Role = Role.Driver });
        _navigation.CurrentRoute = Routes.Products;

        _lifecycle.OnCreated();
        _lifecycle.OnActivated();
        _lifecycle.OnDeactivated();
        _lifecycle.OnResumed();

        Assert.Empty(_preferences.Values);
        Assert.Equal("Ivan Shevchenko", _currentUser.CurrentUser!.FullName);
        Assert.Null(_state.SignedInUserId);
    }
}
