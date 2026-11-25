using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Which drawer entries a role sees, in order. It reads RolePermissions and holds no rule of its own.
// This is the UI layer of authorization: the services still refuse what a role may not do
public static class NavigationMenu
{
    public const string ReceiveTitle = "Receive goods";
    public const string IssueTitle = "Issue goods";

    private static readonly (MenuEntry Entry, Func<Role, bool> Allowed)[] All =
    [
        (new(Routes.Home, "Home", MenuEntryKind.Top, "IconHome"), _ => true),
        (new(Routes.Receive, ReceiveTitle, MenuEntryKind.Action, "IconReceive"),
            role => RolePermissions.CanRecord(role, OperationDirection.In)),
        (new(Routes.Issue, IssueTitle, MenuEntryKind.Action, "IconIssue"),
            role => RolePermissions.CanRecord(role, OperationDirection.Out)),
        (new(Routes.Products, "Products", MenuEntryKind.Top, "IconBox"), _ => true),
        (new(Routes.Map, "Map", MenuEntryKind.Action, "IconMap"), _ => true),
        (new(Routes.Characteristics, "Characteristics", MenuEntryKind.Action, "IconTag"), RolePermissions.CanManageProducts),
        (new(Routes.Settings, "Settings", MenuEntryKind.Top, "IconSettings"), _ => true)
    ];

    public static IReadOnlyList<MenuEntry> For(Role role) =>
        [.. All.Where(item => item.Allowed(role)).Select(item => item.Entry)];
}
