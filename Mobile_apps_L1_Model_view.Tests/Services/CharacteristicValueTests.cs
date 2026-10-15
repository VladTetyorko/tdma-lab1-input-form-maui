using System.Globalization;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class CharacteristicValueTests
{
    [Theory]
    [InlineData("12.5", 12.5)]
    [InlineData(" 12.5 ", 12.5)]
    [InlineData("-3", -3)]
    [InlineData("1e3", 1000)]
    [InlineData("01.50", 1.5)]
    public void TryParseNumber_ValidText_ReturnsTrueAndNumber(string text, double expected)
    {
        var parsed = CharacteristicValue.TryParseNumber(text, out var number);

        Assert.True(parsed);
        Assert.Equal(expected, number);
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("1e400")]
    [InlineData("abc")]
    [InlineData("12kg")]
    [InlineData("1,5")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryParseNumber_NonFiniteOrGarbage_ReturnsFalse(string? text)
    {
        Assert.False(CharacteristicValue.TryParseNumber(text, out _));
    }

    [Fact]
    public void TryParseNumber_UsesInvariantCultureWhenCurrentCultureUsesComma()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.True(CharacteristicValue.TryParseNumber("1.5", out var number));
            Assert.Equal(1.5, number);
            Assert.False(CharacteristicValue.TryParseNumber("1,5", out _));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Normalize_Number_WritesCanonicalForm()
    {
        Assert.Equal("1.5", CharacteristicValue.Normalize(CharacteristicValueType.Number, "01.50"));
    }

    [Fact]
    public void Normalize_Number_TrimsSurroundingSpaces()
    {
        Assert.Equal("7", CharacteristicValue.Normalize(CharacteristicValueType.Number, "  7  "));
    }

    [Fact]
    public void Normalize_Text_OnlyTrims()
    {
        Assert.Equal("Dark Blue", CharacteristicValue.Normalize(CharacteristicValueType.Text, "  Dark Blue  "));
    }

    [Fact]
    public void Normalize_NullText_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, CharacteristicValue.Normalize(CharacteristicValueType.Text, null));
    }
}
