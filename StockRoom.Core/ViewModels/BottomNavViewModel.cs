using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// The bar at the bottom of the root pages: four places and the central + Receive / - Issue pill. What a role gets is
// NavigationMenu's decision; the selected place follows the open page. One instance for the whole app
public sealed partial class BottomNavViewModel : ObservableObject
{
    private readonly ICurrentUserService _currentUser;
    private readonly IStockDocumentDraftStore _drafts;
    private readonly INavigationService _navigation;
    private readonly StockChanges _changes;
    private readonly ILogger<BottomNavViewModel> _logger;

    public BottomNavViewModel(
        ICurrentUserService currentUser,
        IStockDocumentDraftStore drafts,
        INavigationService navigation,
        StockChanges changes,
        ILogger<BottomNavViewModel> logger)
    {
        _currentUser = currentUser;
        _drafts = drafts;
        _navigation = navigation;
        _changes = changes;
        _logger = logger;
        currentUser.Changed += (_, _) => Refresh();
        drafts.Changed += (_, _) => Refresh();
        Refresh();
    }

    // The place that is already open was tapped again: Search clears and focuses its field
    public event EventHandler<string>? Reselected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHome))]
    [NotifyPropertyChangedFor(nameof(IsProducts))]
    [NotifyPropertyChangedFor(nameof(IsSearch))]
    [NotifyPropertyChangedFor(nameof(IsActivity))]
    public partial string Selected { get; set; } = Routes.Home;

    public bool IsHome => Selected == Routes.Home;

    public bool IsProducts => Selected == Routes.Products;

    public bool IsSearch => Selected == Routes.Search;

    public bool IsActivity => Selected == Routes.Activity;

    // A role that may not issue gets one wide Receive button, with no empty half
    [ObservableProperty]
    public partial bool CanIssue { get; set; }

    // The number of lines of an unfinished receipt or issue; empty without one
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReceiveBadge))]
    public partial string ReceiveBadge { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasIssueBadge))]
    public partial string IssueBadge { get; set; } = string.Empty;

    public bool HasReceiveBadge => ReceiveBadge.Length > 0;

    public bool HasIssueBadge => IssueBadge.Length > 0;

    // Called by the shell after every navigation; only root pages have a place in the bar
    public void Select(string route)
    {
        if (Routes.IsRoot(route))
        {
            Selected = route;
        }
    }

    [RelayCommand]
    private async Task OpenAsync(string? route)
    {
        if (route is null || !Offers(route))
        {
            _logger.LogWarning("The bottom bar refused to open {Route} for user {UserId}", route, _currentUser.CurrentUser?.Id);
            return;
        }

        if (route == _navigation.CurrentRoute)
        {
            Reselected?.Invoke(this, route);
            return;
        }

        // Operations recorded elsewhere do not tell the product list, so it reads the products again
        if (route == Routes.Products)
        {
            _changes.Mark();
        }

        await _navigation.GoToAsync(route);
    }

    private bool Offers(string route) =>
        _currentUser.CurrentUser is { } user && NavigationMenu.BottomFor(user.Role).Any(entry => entry.Route == route);

    private void Refresh()
    {
        // After sign-out the bar keeps its last look: only the sign-in page is shown then, which has no bar
        if (_currentUser.CurrentUser is null)
        {
            return;
        }

        CanIssue = Offers(Routes.Issue);
        ReceiveBadge = Badge(OperationDirection.In);
        IssueBadge = Badge(OperationDirection.Out);
    }

    private string Badge(OperationDirection direction) =>
        _drafts.LineCount(direction) is var lines and > 0 ? lines.ToString() : string.Empty;
}
