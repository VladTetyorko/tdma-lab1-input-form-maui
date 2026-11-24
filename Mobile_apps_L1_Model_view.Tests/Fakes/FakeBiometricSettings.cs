using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class FakeBiometricSettings : IBiometricSettings
{
    public bool IsEnabled { get; set; }
}
