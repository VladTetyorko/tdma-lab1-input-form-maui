using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ShakeSettingsTests
{
    private readonly FakePreferencesStore _store = new();

    [Fact]
    public void IsEnabled_IsOnByDefault()
    {
        Assert.True(new ShakeSettings(_store).IsEnabled);
    }

    [Fact]
    public void SwitchingOff_IsRememberedByANewInstance()
    {
        new ShakeSettings(_store).IsEnabled = false;

        Assert.False(new ShakeSettings(_store).IsEnabled);
    }

    [Fact]
    public void SwitchingBackOn_RemovesTheKey()
    {
        var settings = new ShakeSettings(_store);
        settings.IsEnabled = false;

        settings.IsEnabled = true;

        Assert.True(settings.IsEnabled);
        Assert.Empty(_store.Values);
    }
}
