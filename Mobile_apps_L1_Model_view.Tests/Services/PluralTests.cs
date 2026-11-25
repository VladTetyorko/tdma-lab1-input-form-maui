using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class PluralTests
{
    [Theory]
    [InlineData(0, "0 units")]
    [InlineData(1, "1 unit")]
    [InlineData(12, "12 units")]
    public void Units(int count, string expected) => Assert.Equal(expected, Plural.Units(count));

    [Fact]
    public void ProductsAndLines() =>
        Assert.Equal(("1 product", "2 products", "1 line"), (Plural.Products(1), Plural.Products(2), Plural.Lines(1)));
}
