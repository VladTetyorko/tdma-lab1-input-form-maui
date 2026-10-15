using System.Globalization;
using Mobile_apps_L1_Model_view.Converters;

namespace Mobile_apps_L1_Model_view.Tests.Converters;

public class QuantityToColorConverterTests
{
    private readonly QuantityToColorConverter _converter = new();

    [Fact]
    public void Convert_ZeroQuantity_IsRed()
    {
        var result = _converter.Convert(0, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.Equal(Microsoft.Maui.Graphics.Colors.Red, result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(250)]
    public void Convert_PositiveQuantity_KeepsStyleColor(int quantity)
    {
        var result = _converter.Convert(quantity, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.Equal(Microsoft.Maui.Controls.BindableProperty.UnsetValue, result);
    }

    [Fact]
    public void ConvertBack_IsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(Microsoft.Maui.Graphics.Colors.Red, typeof(int), null, CultureInfo.InvariantCulture));
    }
}
