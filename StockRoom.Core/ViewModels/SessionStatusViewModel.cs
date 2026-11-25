using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Who is signed in and how the sync stands: shown in the drawer, on Settings and as a chip on the pages.
// One instance for the whole app, so it holds nothing that belongs to a single page
public sealed partial class SessionStatusViewModel : ObservableObject
{
    private readonly ICurrentUserService _currentUser;
    private readonly ISessionStore _session;
    private readonly IAppStateService _appState;
    private readonly INavigationService _navigation;
    private readonly ILogger<SessionStatusViewModel> _logger;

    public SessionStatusViewModel(
        ICurrentUserService currentUser,
        ISessionStore session,
        IAppStateService appState,
        ConnectivityBannerViewModel connectivity,
        INavigationService navigation,
        ILogger<SessionStatusViewModel> logger)
    {
        _currentUser = currentUser;
        _session = session;
        _appState = appState;
        _navigation = navigation;
        _logger = logger;
        Connectivity = connectivity;
        currentUser.Changed += (_, _) => Refresh();
        Refresh();
    }

    public ConnectivityBannerViewModel Connectivity { get; }

    [ObservableProperty]
    public partial bool IsSignedIn { get; set; }

    [ObservableProperty]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Initials { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RoleText { get; set; } = string.Empty;

    // First letters of the first and last word of the name ("Olena Kovalenko" -> "OK")
    public static string InitialsOf(string fullName)
    {
        var words = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length switch
        {
            0 => string.Empty,
            1 => words[0][..1].ToUpperInvariant(),
            _ => string.Concat(words[0].AsSpan(0, 1), words[^1].AsSpan(0, 1)).ToUpperInvariant()
        };
    }

    [RelayCommand]
    private Task OpenSyncAsync() => _navigation.GoToAsync(Routes.Settings);

    [RelayCommand]
    private async Task SignOutAsync()
    {
        _logger.LogInformation("User {UserId} signed out", _currentUser.CurrentUser?.Id);
        _currentUser.SignOut();
        try
        {
            await _session.ClearAsync();
        }
        catch (Exception ex)
        {
            // Signing out must work even when secure storage does not: the user is signed out in memory anyway
            _logger.LogWarning(ex, "The saved session could not be removed");
        }

        // Drafts (also of unfinished receipts and issues) belong to the user who signs out
        _appState.Clear();
        await _navigation.GoToAsync(Routes.Absolute(Routes.Login));
    }

    private void Refresh()
    {
        var user = _currentUser.CurrentUser;
        IsSignedIn = user is not null;
        UserName = user?.FullName ?? string.Empty;
        Initials = InitialsOf(UserName);
        RoleText = user?.Role.ToString() ?? string.Empty;
        if (user is null)
        {
            Connectivity.Detach();
        }
        else
        {
            Connectivity.Attach();
        }
    }
}
