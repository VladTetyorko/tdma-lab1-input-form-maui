using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class BiometricSettingsTests
{
    private readonly FakePreferencesStore _preferences = new();

    [Fact]
    public void IsOffByDefault()
    {
        Assert.False(new BiometricSettings(_preferences).IsEnabled);
    }

    [Fact]
    public void TurningOn_IsRemembered_ByANewInstance()
    {
        new BiometricSettings(_preferences).IsEnabled = true;

        Assert.True(new BiometricSettings(_preferences).IsEnabled);
    }

    [Fact]
    public void TurningOff_RemovesTheKey()
    {
        var settings = new BiometricSettings(_preferences) { IsEnabled = true };

        settings.IsEnabled = false;

        Assert.False(settings.IsEnabled);
        Assert.Empty(_preferences.Values);
    }
}
