using System.Globalization;
using Mobile_apps_L1_Model_view.Converters;

namespace Mobile_apps_L1_Model_view.Tests;

public class HighScoreToColorConverterTests
{
    private readonly HighScoreToColorConverter _converter = new();

    [Fact]
    public void Convert_ReturnsGreen_WhenValueIsTrue()
    {
        var result = _converter.Convert(true, typeof(Color), null, CultureInfo.InvariantCulture);

        Assert.Equal(Colors.Green, result);
    }

    [Fact]
    public void Convert_ReturnsRed_WhenValueIsFalse()
    {
        var result = _converter.Convert(false, typeof(Color), null, CultureInfo.InvariantCulture);

        Assert.Equal(Colors.Red, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not a bool")]
    [InlineData(42)]
    public void Convert_ReturnsRed_WhenValueIsNotABoolean(object? value)
    {
        var result = _converter.Convert(value, typeof(Color), null, CultureInfo.InvariantCulture);

        Assert.Equal(Colors.Red, result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(Colors.Green, typeof(bool), null, CultureInfo.InvariantCulture));
    }
}