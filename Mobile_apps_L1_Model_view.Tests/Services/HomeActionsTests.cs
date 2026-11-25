using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class HomeActionsTests
{
    [Theory]
    [InlineData(Role.Manager)]
    [InlineData(Role.Storekeeper)]
    public void Tiles_RolesThatMayIssue_GetReceiveAndIssue(Role role)
    {
        Assert.Equal([Routes.Receive, Routes.Issue], HomeActions.Tiles(role).Select(tile => tile.Route));
    }

    [Fact]
    public void Tiles_Driver_GetsReceiveOnly()
    {
        Assert.Equal([Routes.Receive], HomeActions.Tiles(Role.Driver).Select(tile => tile.Route));
    }

    [Fact]
    public void Links_OnlyTheManagerMayAddAProduct()
    {
        Assert.Equal([Routes.Products, Routes.ProductForm], HomeActions.Links(Role.Manager).Select(link => link.Route));
        Assert.Equal([Routes.Products], HomeActions.Links(Role.Storekeeper).Select(link => link.Route));
        Assert.Equal([Routes.Products], HomeActions.Links(Role.Driver).Select(link => link.Route));
    }
}
