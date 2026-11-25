using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Who is signed in and how the sync stands: shown on every main page (header on a phone, side panel on desktop).
// One instance for the whole app, so it holds nothing that belongs to a single page
public sealed partial class SessionStatusViewModel : ObservableObject
{
    private readonly ICurrentUserService _currentUser;
    private readonly ISessionStore _session;
    private readonly IAppStateService _appState;
    private readonly INavigationService _navigation;
    private readonly ILogger<SessionStatusViewModel> _logger;
    private readonly IStockDocumentDraftStore _drafts;

    public SessionStatusViewModel(
        ICurrentUserService currentUser,
        ISessionStore session,
        IAppStateService appState,
        ConnectivityBannerViewModel connectivity,
        INavigationService navigation,
        IStockDocumentDraftStore drafts,
        ILogger<SessionStatusViewModel> logger)
    {
        _drafts = drafts;
        _currentUser = currentUser;
        _session = session;
        _appState = appState;
        _navigation = navigation;
        _logger = logger;
        Connectivity = connectivity;
        currentUser.Changed += (_, _) => Refresh();
        drafts.Changed += (_, _) => RefreshCounts();
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReceiveTitle))]
    public partial int ReceiveDraftCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IssueTitle))]
    public partial int IssueDraftCount { get; set; }

    public string ReceiveTitle => Titled("Receive", ReceiveDraftCount);

    public string IssueTitle => Titled("Issue", IssueDraftCount);

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
    private Task ScanAsync() => _navigation.GoToAsync(Routes.Scanner);

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

    private static string Titled(string title, int lines) => lines > 0 ? $"{title} ({lines})" : title;

    // The drafts belong to the signed-in user: nothing is shown after sign-out
    private void RefreshCounts()
    {
        var signedIn = _currentUser.CurrentUser is not null;
        ReceiveDraftCount = signedIn ? _drafts.LineCount(OperationDirection.In) : 0;
        IssueDraftCount = signedIn ? _drafts.LineCount(OperationDirection.Out) : 0;
    }

    private void Refresh()
    {
        var user = _currentUser.CurrentUser;
        IsSignedIn = user is not null;
        UserName = user?.FullName ?? string.Empty;
        Initials = InitialsOf(UserName);
        RoleText = user?.Role.ToString() ?? string.Empty;
        RefreshCounts();
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
