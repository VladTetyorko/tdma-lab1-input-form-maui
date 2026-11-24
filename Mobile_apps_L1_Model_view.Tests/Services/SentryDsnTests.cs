using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class SentryDsnTests
{
    private const string Dsn = "https://key@o1.ingest.sentry.io/2";

    [Fact]
    public void Resolve_PrefersTheEnvironmentVariable()
    {
        var json = "{ \"Sentry\": { \"Dsn\": \"https://file@o1.ingest.sentry.io/3\" } }";

        Assert.Equal(Dsn, SentryDsn.Resolve($" {Dsn} ", json));
    }

    [Fact]
    public void Resolve_FallsBackToTheSettingsFile()
    {
        Assert.Equal(Dsn, SentryDsn.Resolve(null, $"{{ \"Sentry\": {{ \"Dsn\": \"{Dsn}\" }} }}"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{ \"Sentry\": {} }")]
    [InlineData("{ \"Sentry\": { \"Dsn\": \"\" } }")]
    [InlineData("{ \"Sentry\": { \"Dsn\": 5 } }")]
    public void Resolve_WithNothingUsable_IsNull(string? json)
    {
        Assert.Null(SentryDsn.Resolve(null, json));
    }

    [Fact]
    public void NullCrashReporter_IsDisabledAndDoesNothing()
    {
        var reporter = new NullCrashReporter();

        reporter.Report(new Exception("x"), "test");

        Assert.False(reporter.IsEnabled);
    }

    [Fact]
    public void SentryCrashReporter_WithoutAnInitialisedSdk_IsDisabledAndDoesNotThrow()
    {
        var reporter = new SentryCrashReporter();

        reporter.Report(new Exception("x"), "test");

        Assert.False(reporter.IsEnabled);
    }
}
