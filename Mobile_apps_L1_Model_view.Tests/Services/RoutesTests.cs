using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class RoutesTests
{
    [Theory]
    [InlineData(Routes.Login, "//login")]
    [InlineData(Routes.Products, "//products")]
    public void Absolute_PrefixesRouteWithDoubleSlash(string route, string expected)
    {
        Assert.Equal(expected, Routes.Absolute(route));
    }
}
