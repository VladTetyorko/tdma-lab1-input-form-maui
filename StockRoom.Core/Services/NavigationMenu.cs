using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Which main entries a role sees, in order. It reads RolePermissions and holds no rule of its own.
// This is the UI layer of authorization: the services still refuse what a role may not do
public static class NavigationMenu
{
    public static IReadOnlyList<MenuEntry> For(Role role)
    {
        List<MenuEntry> entries = [new(Routes.Products, "Products")];
        if (RolePermissions.CanRecord(role, OperationDirection.In))
        {
            entries.Add(new MenuEntry(Routes.Receive, "Receive"));
        }

        if (RolePermissions.CanRecord(role, OperationDirection.Out))
        {
            entries.Add(new MenuEntry(Routes.Issue, "Issue"));
        }

        entries.Add(new MenuEntry(Routes.Settings, "Settings"));
        return entries;
    }
}
