using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class SessionStatusViewModelTests
{
    private const string DrillBarcode = "4006381333931";

    private readonly TestStore _store = new();
    private readonly SessionStatusViewModel _status;

    public SessionStatusViewModelTests()
    {
        _status = new SessionStatusViewModel(_store.CurrentUser, _store.Session, _store.AppState, _store.Banner,
            _store.Navigation, _store.Logger<SessionStatusViewModel>());
    }

    [Theory]
    [InlineData("Olena Kovalenko", "OK")]
    [InlineData("petro", "P")]
    [InlineData("Anna Maria Shevchenko", "AS")]
    [InlineData("", "")]
    public void InitialsOf_TakesFirstAndLastWord(string name, string expected)
    {
        Assert.Equal(expected, SessionStatusViewModel.InitialsOf(name));
    }

    [Fact]
    public async Task SigningIn_ShowsTheUser()
    {
        await _store.SignInAsync(Role.Manager, "Olena Kovalenko");

        Assert.True(_status.IsSignedIn);
        Assert.Equal("Olena Kovalenko", _status.UserName);
        Assert.Equal("OK", _status.Initials);
        Assert.Equal("Manager", _status.RoleText);
    }

    [Fact]
    public async Task SignedIn_TheSyncStateFollowsTheQueueAndEvents()
    {
        _store.Sync.Pending = 2;

        await _store.SignInAsync(Role.Storekeeper);

        await Eventually.WaitAsync(() => _status.Connectivity.Text == "2 changes waiting to sync");
        _store.Sync.RaiseStarted();
        Assert.Equal("Syncing...", _status.Connectivity.Text);
    }

    [Fact]
    public async Task SigningOut_ClearsTheUserAndStopsListening()
    {
        await _store.SignInAsync(Role.Storekeeper);
        await Eventually.WaitAsync(() => _status.Connectivity.Text.Length > 0);

        await _status.SignOutCommand.ExecuteAsync(null);
        _store.Sync.RaiseStarted();

        Assert.False(_status.IsSignedIn);
        Assert.Equal(string.Empty, _status.UserName);
        Assert.NotEqual(ConnectivityState.Syncing, _status.Connectivity.State);
    }

    [Fact]
    public async Task SignOut_ClearsTheSavedSessionAndDraftsAndGoesToLogin()
    {
        var manager = await _store.SignInAsync(Role.Manager);
        await _store.Session.SaveAsync(manager);
        _store.AppState.SetDraft(new ProductFormDraft("Saw", DrillBarcode));
        _store.AppState.Save();

        await _status.SignOutCommand.ExecuteAsync(null);

        Assert.Null(_store.CurrentUser.CurrentUser);
        Assert.False(_store.Secrets.Values.ContainsKey(SessionStore.TokenKey));
        Assert.Null(_store.AppState.GetDraft<ProductFormDraft>());
        Assert.Empty(_store.Preferences.Values);
        Assert.Equal(Routes.Absolute(Routes.Login), Assert.Single(_store.Navigation.RequestedRoutes));
    }

    [Fact]
    public async Task SignOut_SecretStoreFails_StillSignsOutAndGoesToLogin()
    {
        var manager = await _store.SignInAsync(Role.Manager);
        await _store.Session.SaveAsync(manager);
        _store.Secrets.Failing = true;

        await _status.SignOutCommand.ExecuteAsync(null);

        Assert.Null(_store.CurrentUser.CurrentUser);
        Assert.Equal(Routes.Absolute(Routes.Login), Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Contains(_store.Logs, l => l.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task ScanAndOpenSync_Navigate()
    {
        await _status.ScanCommand.ExecuteAsync(null);
        await _status.OpenSyncCommand.ExecuteAsync(null);

        Assert.Equal([Routes.Scanner, Routes.Settings], _store.Navigation.RequestedRoutes);
    }
}
