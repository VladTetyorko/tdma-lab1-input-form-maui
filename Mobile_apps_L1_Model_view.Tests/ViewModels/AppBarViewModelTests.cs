using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class AppBarViewModelTests
{
    private readonly FakeNavigationDrawer _drawer = new();
    private readonly FakeNavigationService _navigation = new();

    private AppBarViewModel Bar => new(_drawer, _navigation);

    [Theory]
    [InlineData(Routes.Home, true)]
    [InlineData(Routes.Products, true)]
    [InlineData(Routes.Search, true)]
    [InlineData(Routes.Activity, true)]
    [InlineData(Routes.Settings, false)]
    [InlineData(Routes.Receive, false)]
    [InlineData(Routes.ProductDetail, false)]
    [InlineData(null, false)]
    public async Task TheLeftButton_OpensTheMenuOnRootPagesOnly(string? route, bool root)
    {
        await Bar.LeftAsync(route, null);

        Assert.Equal(root, _drawer.IsOpen);
        Assert.Equal(root ? [] : [Routes.Back], _navigation.RequestedRoutes);
    }

    [Fact]
    public async Task APageWithItsOwnWayBack_IsAsked()
    {
        var asked = false;

        await Bar.LeftAsync(null, new RelayCommand(() => asked = true));

        Assert.True(asked);
        Assert.Empty(_navigation.RequestedRoutes);
    }
}
