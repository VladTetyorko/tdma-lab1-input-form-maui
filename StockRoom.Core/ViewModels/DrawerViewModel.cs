using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// The drawer: the entries of the signed-in role (NavigationMenu decides), the user card and sync state (Status), the
// close button and sign-out. One instance for the whole app
public sealed partial class DrawerViewModel : ObservableObject
{
    private readonly ICurrentUserService _currentUser;
    private readonly IStockDocumentDraftStore _drafts;
    private readonly INavigationService _navigation;
    private readonly INavigationDrawer _drawer;
    private readonly StockChanges _changes;
    private readonly ILogger<DrawerViewModel> _logger;
    private string _openRoute = string.Empty;

    public DrawerViewModel(
        SessionStatusViewModel status,
        ICurrentUserService currentUser,
        IStockDocumentDraftStore drafts,
        INavigationService navigation,
        INavigationDrawer drawer,
        StockChanges changes,
        ILogger<DrawerViewModel> logger)
    {
        Status = status;
        _currentUser = currentUser;
        _drafts = drafts;
        _navigation = navigation;
        _drawer = drawer;
        _changes = changes;
        _logger = logger;
        currentUser.Changed += (_, _) => Rebuild();
        drafts.Changed += (_, _) => ApplyDraftCounts();
        Rebuild();
    }

    public SessionStatusViewModel Status { get; }

    public ObservableCollection<DrawerItem> Items { get; } = [];

    // Called by the shell after every navigation, so the open page is highlighted
    public void Select(string route)
    {
        _openRoute = route;
        foreach (var item in Items)
        {
            item.IsSelected = item.Route == route;
        }
    }

    [RelayCommand]
    private void Close() => _drawer.Close();

    [RelayCommand]
    private async Task OpenAsync(DrawerItem? item)
    {
        if (item is null)
        {
            return;
        }

        _drawer.Close();
        if (_currentUser.CurrentUser is not { } user || NavigationMenu.For(user.Role).All(entry => entry.Route != item.Route))
        {
            _logger.LogWarning("Menu refused to open {Route} for user {UserId}", item.Route, _currentUser.CurrentUser?.Id);
            return;
        }

        // A pushed page that is already open is not pushed a second time
        if (item.Kind == MenuEntryKind.Action && item.Route == _openRoute)
        {
            return;
        }

        // Pages opened elsewhere do not tell the product list what they changed, so it reads the products again
        if (item.Route is Routes.Products or Routes.Characteristics)
        {
            _changes.Mark();
        }

        await _navigation.GoToAsync(item.Route);
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        _drawer.Close();
        await Status.SignOutCommand.ExecuteAsync(null);
    }

    // After sign-out the last menu stays: only the sign-in page is on screen then, where the drawer is switched off
    private void Rebuild()
    {
        if (_currentUser.CurrentUser is not { } user)
        {
            return;
        }

        Items.Clear();
        foreach (var entry in NavigationMenu.For(user.Role))
        {
            Items.Add(new DrawerItem(entry));
        }

        ApplyDraftCounts();
        Select(_openRoute);
    }

    // A half-done delivery is never forgotten: "Receive goods (5)"
    private void ApplyDraftCounts()
    {
        foreach (var item in Items)
        {
            var lines = _currentUser.CurrentUser is null ? 0 : item.Route switch
            {
                Routes.Receive => _drafts.LineCount(OperationDirection.In),
                Routes.Issue => _drafts.LineCount(OperationDirection.Out),
                _ => 0
            };
            item.Title = lines > 0 ? $"{item.BaseTitle} ({lines})" : item.BaseTitle;
        }
    }
}
