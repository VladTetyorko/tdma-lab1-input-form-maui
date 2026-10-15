using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class Ean13Tests
{
    [Theory]
    [InlineData("4006381333931")]
    [InlineData("4820001000016")]
    public void IsValid_AcceptsCodeWithCorrectCheckDigit(string code)
    {
        Assert.True(Ean13.IsValid(code));
    }

    [Theory]
    [InlineData("4006381333932")]
    [InlineData("4820001000010")]
    public void IsValid_RejectsWrongCheckDigit(string code)
    {
        Assert.False(Ean13.IsValid(code));
    }

    [Theory]
    [InlineData("400638133393")]
    [InlineData("40063813339311")]
    [InlineData("4006381333931 ")]
    public void IsValid_RejectsWrongLength(string code)
    {
        Assert.False(Ean13.IsValid(code));
    }

    [Theory]
    [InlineData("400638133393A")]
    [InlineData("40063813339-1")]
    [InlineData("4006381333 31")]
    public void IsValid_RejectsNonDigits(string code)
    {
        Assert.False(Ean13.IsValid(code));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_RejectsNullEmptyOrBlank(string? code)
    {
        Assert.False(Ean13.IsValid(code));
    }

    [Fact]
    public void ComputeCheckDigit_ReturnsExpectedDigit()
    {
        Assert.Equal(1, Ean13.ComputeCheckDigit("400638133393"));
        Assert.Equal(6, Ean13.ComputeCheckDigit("482000100001"));
    }

    [Theory]
    [InlineData("40063813339")]
    [InlineData("4006381333933")]
    [InlineData("40063813339X")]
    [InlineData("")]
    public void ComputeCheckDigit_RejectsInputThatIsNot12Digits(string digits)
    {
        Assert.Throws<ArgumentException>(() => Ean13.ComputeCheckDigit(digits));
    }

    [Fact]
    public void WithCheckDigit_AppendsCheckDigitAndProducesValidCode()
    {
        var code = Ean13.WithCheckDigit("400638133393");

        Assert.Equal("4006381333931", code);
        Assert.True(Ean13.IsValid(code));
    }

    [Fact]
    public void WithCheckDigit_RejectsInputThatIsNot12Digits()
    {
        Assert.Throws<ArgumentException>(() => Ean13.WithCheckDigit("123"));
    }
}
