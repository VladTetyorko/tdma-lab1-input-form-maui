using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class SettingsShakeTests
{
    private readonly TestStore _store = new();
    private readonly FakeBiometricSettings _biometricSettings = new();

    private SettingsViewModel CreateViewModel() => new(
        new ThemeService(_store.Preferences, new FakeThemeApplier()), _store.DeepLinks, _store.Sync, _store.Offline,
        new FakeDatabaseExporter(), _store.Diagnostics, _biometricSettings, new FakeBiometricAuthenticator(),
        new ShakeSettings(_store.Preferences), _store.ShakeDetector, new ScanSoundSettings(_store.Preferences), _store.Navigation, _store.Logger<SettingsViewModel>());

    [Fact]
    public void ShakeToScan_IsOnByDefault()
    {
        Assert.True(CreateViewModel().ShakeToScan);
    }

    [Fact]
    public void SwitchingItOff_IsSaved_LoggedAndRemembered()
    {
        var viewModel = CreateViewModel();

        viewModel.ShakeToScan = false;

        Assert.False(new ShakeSettings(_store.Preferences).IsEnabled);
        Assert.Contains(_store.Logs, l => l.Message.Contains("Shake to scan switched off"));
        Assert.False(CreateViewModel().ShakeToScan);
    }

    [Fact]
    public void SwitchingItOn_Again_IsSaved()
    {
        var viewModel = CreateViewModel();
        viewModel.ShakeToScan = false;

        viewModel.ShakeToScan = true;

        Assert.True(new ShakeSettings(_store.Preferences).IsEnabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsShakeSupported_FollowsTheAccelerometer(bool supported)
    {
        _store.ShakeDetector.IsSupported = supported;

        var viewModel = CreateViewModel();

        Assert.Equal(supported, viewModel.IsShakeSupported);
        Assert.Equal(!supported, viewModel.IsShakeUnsupported);
    }
}
