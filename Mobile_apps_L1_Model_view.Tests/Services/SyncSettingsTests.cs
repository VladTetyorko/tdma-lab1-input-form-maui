using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class SyncSettingsTests
{
    [Fact]
    public void Resolve_ReadsTheBaseUrlAndThePins()
    {
        var json = "{ \"Sync\": { \"BaseUrl\": \"https://sync.example.com/\", \"Pins\": [\"AAA=\", \" sha256/BBB= \", \"\", 5] } }";

        var settings = SyncSettings.Resolve(json);

        Assert.True(settings.IsConfigured);
        Assert.Equal(new Uri("https://sync.example.com/"), settings.BaseUrl);
        Assert.Equal(["AAA=", "sha256/BBB="], settings.Pins);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{ \"Sync\": {} }")]
    [InlineData("{ \"Sync\": { \"BaseUrl\": \"\" } }")]
    [InlineData("{ \"Sync\": { \"BaseUrl\": \"not a url\" } }")]
    [InlineData("{ \"Sync\": { \"BaseUrl\": \"http://sync.example.com/\" } }")]
    [InlineData("{ \"Sync\": { \"BaseUrl\": 5 } }")]
    public void Resolve_WithoutAUsableHttpsUrl_IsNotConfigured_SoTheStubStaysInUse(string? json)
    {
        Assert.False(SyncSettings.Resolve(json).IsConfigured);
    }

    [Fact]
    public void Resolve_UrlWithoutPins_IsConfigured_ButHasNoPins()
    {
        var settings = SyncSettings.Resolve("{ \"Sync\": { \"BaseUrl\": \"https://sync.example.com/\" } }");

        Assert.True(settings.IsConfigured);
        Assert.Empty(settings.Pins);
    }

    [Fact]
    public void SettingsFile_IsSharedBySentryAndSync()
    {
        var json = "{ \"Sentry\": { \"Dsn\": \"https://k@o1.ingest.sentry.io/2\" }, \"Sync\": { \"BaseUrl\": \"https://s.example.com/\" } }";

        Assert.Equal("https://k@o1.ingest.sentry.io/2", SentryDsn.Resolve(null, json));
        Assert.True(SyncSettings.Resolve(json).IsConfigured);
        Assert.Equal("appsettings.local.json", LocalSettings.FileName);
    }
}
