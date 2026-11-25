using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Where a role can go: the places of the bottom bar and the rare entries of the drawer. It reads RolePermissions and
// holds no rule of its own. This is the UI layer of authorization: the services still refuse what a role may not do
public static class NavigationMenu
{
    private static readonly (MenuEntry Entry, Func<Role, bool> Allowed)[] Bottom =
    [
        (new(Routes.Home, "Home", MenuEntryKind.Top, "IconHome"), _ => true),
        (new(Routes.Products, "Products", MenuEntryKind.Top, "IconBox"), _ => true),
        (new(Routes.Receive, "Receive goods", MenuEntryKind.Action, "IconAdd"),
            role => RolePermissions.CanRecord(role, OperationDirection.In)),
        (new(Routes.Issue, "Issue goods", MenuEntryKind.Action, "IconSubtract"),
            role => RolePermissions.CanRecord(role, OperationDirection.Out)),
        (new(Routes.Search, "Search", MenuEntryKind.Top, "IconSearch"), _ => true),
        (new(Routes.Activity, "Activity", MenuEntryKind.Top, "IconHistory"), _ => true)
    ];

    private static readonly (MenuEntry Entry, Func<Role, bool> Allowed)[] Drawer =
    [
        (new(Routes.Characteristics, "Characteristics", MenuEntryKind.Action, "IconTag"), RolePermissions.CanManageProducts),
        (new(Routes.Map, "Operations map", MenuEntryKind.Action, "IconMap"), _ => true),
        (new(Routes.Settings, "Settings", MenuEntryKind.Action, "IconSettings"), _ => true)
    ];

    // The bar at the bottom of the root pages, left to right; the two actions are the halves of the central pill
    public static IReadOnlyList<MenuEntry> BottomFor(Role role) => For(Bottom, role);

    public static IReadOnlyList<MenuEntry> DrawerFor(Role role) => For(Drawer, role);

    private static IReadOnlyList<MenuEntry> For((MenuEntry Entry, Func<Role, bool> Allowed)[] all, Role role) =>
        [.. all.Where(item => item.Allowed(role)).Select(item => item.Entry)];
}
