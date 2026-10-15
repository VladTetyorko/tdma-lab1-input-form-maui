using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ResultTests
{
    [Fact]
    public void Success_CarriesValueAndNoError()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_CarriesErrorAndNoValue()
    {
        var result = Result<string>.Failure("Something went wrong.");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("Something went wrong.", result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Failure_RequiresAMessage(string error)
    {
        Assert.Throws<ArgumentException>(() => Result<int>.Failure(error));
    }
}
