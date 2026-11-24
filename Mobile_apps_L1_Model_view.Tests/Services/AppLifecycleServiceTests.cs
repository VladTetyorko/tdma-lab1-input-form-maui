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
    private readonly FakeSecretStore _secrets = new();
    private readonly FakeClock _clock = new();
    private readonly FakeBiometricSettings _biometric = new();
    private readonly CurrentUserService _currentUser = new();
    private readonly FakeNavigationService _navigation = new();
    private readonly FakeSyncService _sync = new();
    private readonly SessionStore _session;
    private readonly AppStateService _state;
    private readonly AppLifecycleService _lifecycle;

    public AppLifecycleServiceTests()
    {
        _state = new AppStateService(_preferences);
        var theme = new ThemeService(_preferences, _applier);
        _session = new SessionStore(_secrets, new HmacTokenService(_secrets, _clock), _users,
            NullLogger<SessionStore>.Instance);
        _lifecycle = new AppLifecycleService(_state, _currentUser, theme, _session, _biometric, _navigation, _sync,
            NullLogger<AppLifecycleService>.Instance);
    }

    private async Task<User> SaveSessionOfNewUserAsync()
    {
        var user = new User { FullName = "Petro Bondarenko", Role = Role.Storekeeper, Login = "petro" };
        await _users.AddAsync(user);
        await _session.SaveAsync(user);
        return user;
    }

    [Fact]
    public async Task RestoreAsync_ValidSession_SignsInAgainAndReturnsProducts()
    {
        var user = await SaveSessionOfNewUserAsync();

        var route = await _lifecycle.RestoreAsync();

        Assert.Equal(Routes.Products, route);
        Assert.Equal(user.Id, _currentUser.CurrentUser?.Id);
        Assert.Equal(Role.Storekeeper, _currentUser.CurrentRole);
    }

    [Fact]
    public async Task RestoreAsync_SavedRoute_IsOfferedForResumeAfterStart()
    {
        await SaveSessionOfNewUserAsync();
        _state.LastRoute = Routes.Operation;
        _state.Save();

        await _lifecycle.RestoreAsync();

        Assert.Equal(Routes.Operation, _state.TakeResumeRoute());
    }

    [Fact]
    public async Task RestoreAsync_SavedUserIdWithoutAToken_DoesNotSignAnyoneIn()
    {
        // What the old version saved: a bare user id. It is no longer trusted (and no longer read)
        var user = new User { FullName = "Petro Bondarenko", Role = Role.Storekeeper };
        await _users.AddAsync(user);
        _preferences.Values["state.user"] = user.Id.ToString();

        var route = await _lifecycle.RestoreAsync();

        Assert.Equal(Routes.Login, route);
        Assert.Null(_currentUser.CurrentUser);
    }

    [Fact]
    public async Task RestoreAsync_TamperedToken_ReturnsLoginAndClearsTheSavedState()
    {
        await SaveSessionOfNewUserAsync();
        _secrets.Values[SessionStore.TokenKey] = _secrets.Values[SessionStore.TokenKey][..^2] + "AA";
        _state.LastRoute = Routes.ProductDetail;
        _state.SetDraft(new ProductFormDraft("Saw", "4820001000016"));
        _state.Save();

        var route = await _lifecycle.RestoreAsync();

        Assert.Equal(Routes.Login, route);
        Assert.Null(_currentUser.CurrentUser);
        Assert.Empty(_preferences.Values);
        Assert.Null(_state.TakeResumeRoute());
        Assert.False(_secrets.Values.ContainsKey(SessionStore.TokenKey));
    }

    [Fact]
    public async Task RestoreAsync_ExpiredToken_ReturnsLogin()
    {
        await SaveSessionOfNewUserAsync();
        _clock.UtcNow += HmacTokenService.Lifetime + TimeSpan.FromDays(1);

        Assert.Equal(Routes.Login, await _lifecycle.RestoreAsync());
        Assert.Null(_currentUser.CurrentUser);
    }

    [Fact]
    public async Task RestoreAsync_TokenOfAUserWhoNoLongerExists_ReturnsLogin()
    {
        await _session.SaveAsync(new User { FullName = "Ghost", Role = Role.Driver });

        Assert.Equal(Routes.Login, await _lifecycle.RestoreAsync());
        Assert.Null(_currentUser.CurrentUser);
    }

    [Fact]
    public async Task RestoreAsync_SecretStoreUnavailable_ReturnsLoginInsteadOfCrashing()
    {
        await SaveSessionOfNewUserAsync();
        _secrets.Failing = true;

        Assert.Equal(Routes.Login, await _lifecycle.RestoreAsync());
        Assert.Null(_currentUser.CurrentUser);
    }

    [Fact]
    public async Task RestoreAsync_NothingSaved_ReturnsLogin()
    {
        var route = await _lifecycle.RestoreAsync();

        Assert.Equal(Routes.Login, route);
        Assert.Null(_currentUser.CurrentUser);
    }

    [Fact]
    public async Task RestoreAsync_BiometricUnlockOn_ValidSessionWaitsOnTheLoginPage_AndKeepsTheSavedState()
    {
        await SaveSessionOfNewUserAsync();
        _biometric.IsEnabled = true;
        _state.LastRoute = Routes.Operation;
        _state.SetDraft(new ProductFormDraft("Saw", "4820001000016"));
        _state.Save();

        var route = await _lifecycle.RestoreAsync();

        Assert.Equal(Routes.Login, route);
        Assert.Null(_currentUser.CurrentUser);
        Assert.True(await _session.HasValidSessionAsync());
        Assert.NotNull(_state.GetDraft<ProductFormDraft>());
        Assert.Equal(Routes.Operation, _state.TakeResumeRoute());
    }

    [Fact]
    public async Task RestoreAsync_BiometricUnlockOn_NoSession_ReturnsLogin()
    {
        _biometric.IsEnabled = true;

        Assert.Equal(Routes.Login, await _lifecycle.RestoreAsync());
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
    public void OnStopped_SavesTheCurrentRoute_ButNoUser()
    {
        var user = new User { FullName = "Petro Bondarenko", Role = Role.Storekeeper };
        _currentUser.SignIn(user);
        _navigation.CurrentRoute = Routes.ProductDetail;

        _lifecycle.OnStopped();

        var restored = new AppStateService(_preferences);
        restored.Restore();
        Assert.False(_preferences.Values.ContainsKey("state.user"));
        Assert.Equal(Routes.ProductDetail, restored.LastRoute);
    }

    [Fact]
    public void OnDestroying_SavesTheCurrentRoute_ButNoUser()
    {
        var user = new User { FullName = "Olena Kovalenko", Role = Role.Manager };
        _currentUser.SignIn(user);
        _navigation.CurrentRoute = Routes.Scanner;

        _lifecycle.OnDestroying();

        var restored = new AppStateService(_preferences);
        restored.Restore();
        Assert.False(_preferences.Values.ContainsKey("state.user"));
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
    }

    [Fact]
    public async Task RestoreAsync_StartsSynchronisation()
    {
        await _lifecycle.RestoreAsync();

        Assert.Equal(1, _sync.StartCalls);
    }

    [Fact]
    public void OnResumed_AsksForASync()
    {
        _lifecycle.OnResumed();

        Assert.Equal(1, _sync.RequestCalls);
    }

    [Fact]
    public void OnStoppedAndOtherEvents_DoNotAskForASync()
    {
        _lifecycle.OnStopped();
        _lifecycle.OnDeactivated();
        _lifecycle.OnActivated();

        Assert.Equal(0, _sync.RequestCalls);
    }
}
