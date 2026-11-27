namespace Mobile_apps_L1_Model_view.Services;

// The "Biometric unlock" switch from Settings. Not a secret, so it lives in preferences. Off by default
public interface IBiometricSettings
{
    bool IsEnabled { get; set; }
}
