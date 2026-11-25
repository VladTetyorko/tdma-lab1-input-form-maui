using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class NavigationMenuTests
{
    private static IEnumerable<string> Bottom(Role role) => NavigationMenu.BottomFor(role).Select(entry => entry.Route);

    private static IEnumerable<string> Drawer(Role role) => NavigationMenu.DrawerFor(role).Select(entry => entry.Route);

    [Theory]
    [InlineData(Role.Manager)]
    [InlineData(Role.Storekeeper)]
    public void BottomFor_StaffSeeBothActionsInTheMiddle(Role role)
    {
        Assert.Equal([Routes.Home, Routes.Products, Routes.Receive, Routes.Issue, Routes.Search, Routes.Activity], Bottom(role));
    }

    [Fact]
    public void BottomFor_Driver_HasNoIssue()
    {
        Assert.Equal([Routes.Home, Routes.Products, Routes.Receive, Routes.Search, Routes.Activity], Bottom(Role.Driver));
    }

    [Fact]
    public void DrawerFor_KeepsOnlyTheRareEntries()
    {
        Assert.Equal([Routes.Characteristics, Routes.Map, Routes.Settings], Drawer(Role.Manager));
        Assert.Equal([Routes.Map, Routes.Settings], Drawer(Role.Storekeeper));
        Assert.Equal([Routes.Map, Routes.Settings], Drawer(Role.Driver));
        Assert.All(NavigationMenu.DrawerFor(Role.Manager), entry => Assert.StartsWith("Icon", entry.Glyph));
    }
}
