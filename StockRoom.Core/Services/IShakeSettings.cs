namespace Mobile_apps_L1_Model_view.Services;

// The "Shake to scan" switch from Settings. Not a secret, so it lives in preferences. On by default
public interface IShakeSettings
{
    bool IsEnabled { get; set; }
}
