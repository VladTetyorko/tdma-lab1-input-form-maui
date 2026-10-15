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

    [Theory]
    [InlineData("//products/productform", "productform")]
    [InlineData("//products", "products")]
    [InlineData("//login", "login")]
    [InlineData("productdetail", "productdetail")]
    public void Leaf_ReturnsTheRouteThatIsOpen(string location, string expected)
    {
        Assert.Equal(expected, Routes.Leaf(location));
    }

    [Fact]
    public void Leaf_NoLocation_IsEmpty()
    {
        Assert.Equal(string.Empty, Routes.Leaf(null));
        Assert.Equal(string.Empty, Routes.Leaf(string.Empty));
    }
}
