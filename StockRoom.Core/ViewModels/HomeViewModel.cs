using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// The start page: the two daily jobs as big tiles, a few links and the recent activity. Which of them exist is decided by
// HomeActions. The commands refuse the same things, and the services behind them refuse what a role may not do
public sealed partial class HomeViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly ICurrentUserService _currentUser;
    private readonly IStockDocumentDraftStore _drafts;
    private readonly IStartupNavigator _startup;
    private readonly IShakeGesture _shake;
    private readonly INavigationService _navigation;
    private readonly IClock _clock;
    private readonly StockChanges _changes;
    private readonly ILogger<HomeViewModel> _logger;

    public HomeViewModel(
        ICurrentUserService currentUser,
        IStockDocumentDraftStore drafts,
        IStartupNavigator startup,
        IShakeGesture shake,
        INavigationService navigation,
        IClock clock,
        StockChanges changes,
        RecentActivityViewModel recent,
        ILogger<HomeViewModel> logger)
    {
        _currentUser = currentUser;
        _drafts = drafts;
        _startup = startup;
        _shake = shake;
        _navigation = navigation;
        _clock = clock;
        _changes = changes;
        _logger = logger;
        Recent = recent;
        drafts.Changed += (_, _) => RefreshCaptions();
        Refresh();
    }

    public RecentActivityViewModel Recent { get; }

    [ObservableProperty]
    public partial string Greeting { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RoleText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool CanReceive { get; set; }

    [ObservableProperty]
    public partial bool CanIssue { get; set; }

    [ObservableProperty]
    public partial bool CanAddProduct { get; set; }

    // Empty when the role may issue goods
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasIssueNote))]
    public partial string IssueNote { get; set; } = string.Empty;

    public bool HasIssueNote => IssueNote.Length > 0;

    [ObservableProperty]
    public partial string ReceiveCaption { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string IssueCaption { get; set; } = string.Empty;

    public void OnAppearing() => AppearCommand.Execute(null);

    public void OnDisappearing() => _shake.Stop();

    // Home is the first page after sign-in, so a deep link or an unfinished page of the last run continues from here
    [RelayCommand]
    private async Task AppearAsync()
    {
        Refresh();
        _shake.Start(() => _navigation.GoToAsync(Routes.Scanner));
        await Recent.RefreshCommand.ExecuteAsync(null);
        await _startup.RunAsync();
    }

    [RelayCommand]
    private Task OpenReceiveAsync() => OpenAsync(Routes.Receive);

    [RelayCommand]
    private Task OpenIssueAsync() => OpenAsync(Routes.Issue);

    [RelayCommand]
    private Task FindProductAsync() => OpenAsync(Routes.Products);

    [RelayCommand]
    private Task AddProductAsync() => OpenAsync(Routes.ProductForm);

    [RelayCommand]
    private Task ScanAsync() => _navigation.GoToAsync(Routes.Scanner);

    private Task OpenAsync(string route)
    {
        if (_currentUser.CurrentUser is not { } user || !Offers(user.Role, route))
        {
            _logger.LogWarning("Home refused to open {Route} for user {UserId}", route, _currentUser.CurrentUser?.Id);
            return Task.CompletedTask;
        }

        // Pages opened from here do not tell the product list what they changed, so it reads the products again
        if (route == Routes.Products)
        {
            _changes.Mark();
        }

        return _navigation.GoToAsync(route);
    }

    private static bool Offers(Role role, string route) =>
        HomeActions.Tiles(role).Concat(HomeActions.Links(role)).Any(action => action.Route == route);

    private void Refresh()
    {
        var user = _currentUser.CurrentUser;
        CanReceive = user is not null && Offers(user.Role, Routes.Receive);
        CanIssue = user is not null && Offers(user.Role, Routes.Issue);
        CanAddProduct = user is not null && Offers(user.Role, Routes.ProductForm);
        IssueNote = user is not null && !CanIssue ? HomeActions.IssueNotAllowed : string.Empty;
        RoleText = user?.Role.ToString() ?? string.Empty;
        Greeting = user is null ? string.Empty : $"{TimeOfDay()}, {user.FullName.Split(' ')[0]}";
        RefreshCaptions();
    }

    private string TimeOfDay() => _clock.UtcNow.ToLocalTime().Hour switch
    {
        < 12 => "Good morning",
        < 18 => "Good afternoon",
        _ => "Good evening"
    };

    private void RefreshCaptions()
    {
        ReceiveCaption = Caption(OperationDirection.In, HomeActions.ReceiveHint);
        IssueCaption = Caption(OperationDirection.Out, HomeActions.IssueHint);
    }

    // "Draft: 5 lines · continue" while a receipt or issue is unfinished
    private string Caption(OperationDirection direction, string hint)
    {
        var lines = _currentUser.CurrentUser is null ? 0 : _drafts.LineCount(direction);
        return lines == 0 ? hint : $"Draft: {lines} {(lines == 1 ? "line" : "lines")} · continue";
    }
}
