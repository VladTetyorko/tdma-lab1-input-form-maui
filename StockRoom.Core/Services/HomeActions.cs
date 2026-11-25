using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// What Home offers a role: the big tiles (daily work), the smaller links and, when a tile is missing, why
public static class HomeActions
{
    public const string IssueNotAllowed = "Issuing goods is done by a storekeeper or a manager.";
    public const string ReceiveHint = "Add delivered goods";
    public const string IssueHint = "Hand goods out";

    public static IReadOnlyList<HomeAction> Tiles(Role role)
    {
        List<HomeAction> tiles = [];
        if (RolePermissions.CanRecord(role, OperationDirection.In))
        {
            tiles.Add(new HomeAction(Routes.Receive, NavigationMenu.ReceiveTitle, ReceiveHint));
        }

        if (RolePermissions.CanRecord(role, OperationDirection.Out))
        {
            tiles.Add(new HomeAction(Routes.Issue, NavigationMenu.IssueTitle, IssueHint));
        }

        return tiles;
    }

    public static IReadOnlyList<HomeAction> Links(Role role)
    {
        List<HomeAction> links = [new(Routes.Products, "Find a product")];
        if (RolePermissions.CanManageProducts(role))
        {
            links.Add(new HomeAction(Routes.ProductForm, "Add product"));
        }

        return links;
    }
}
