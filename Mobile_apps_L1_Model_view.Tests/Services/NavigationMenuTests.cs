using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class NavigationMenuTests
{
    private static IEnumerable<string> RoutesFor(Role role) => NavigationMenu.For(role).Select(entry => entry.Route);

    [Theory]
    [InlineData(Role.Manager)]
    [InlineData(Role.Storekeeper)]
    public void For_RolesThatMayIssue_SeeEveryEntryInOrder(Role role)
    {
        Assert.Equal([Routes.Products, Routes.Receive, Routes.Issue, Routes.Settings], RoutesFor(role));
    }

    [Fact]
    public void For_Driver_HasNoIssue()
    {
        Assert.Equal([Routes.Products, Routes.Receive, Routes.Settings], RoutesFor(Role.Driver));
    }
}
