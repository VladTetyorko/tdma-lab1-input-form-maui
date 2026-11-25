using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class NavigationMenuTests
{
    private static IEnumerable<string> RoutesFor(Role role) => NavigationMenu.For(role).Select(entry => entry.Route);

    [Fact]
    public void For_Manager_SeesEveryEntryInOrder()
    {
        Assert.Equal(
            [Routes.Home, Routes.Receive, Routes.Issue, Routes.Products, Routes.Map, Routes.Characteristics, Routes.Settings],
            RoutesFor(Role.Manager));
    }

    [Fact]
    public void For_Storekeeper_HasNoCharacteristics()
    {
        Assert.Equal([Routes.Home, Routes.Receive, Routes.Issue, Routes.Products, Routes.Map, Routes.Settings],
            RoutesFor(Role.Storekeeper));
    }

    [Fact]
    public void For_Driver_HasNoIssueAndNoCharacteristics()
    {
        Assert.Equal([Routes.Home, Routes.Receive, Routes.Products, Routes.Map, Routes.Settings], RoutesFor(Role.Driver));
    }

    [Fact]
    public void OnlyHomeProductsAndSettingsAreTopLevelPages()
    {
        var top = NavigationMenu.For(Role.Manager).Where(entry => entry.Kind == MenuEntryKind.Top).Select(entry => entry.Route);

        Assert.Equal([Routes.Home, Routes.Products, Routes.Settings], top);
        Assert.All(NavigationMenu.For(Role.Manager), entry => Assert.StartsWith("Icon", entry.Glyph));
    }
}
