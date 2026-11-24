namespace Mobile_apps_L1_Model_view.Services;

public sealed class ShakeSettings(IPreferencesStore store) : IShakeSettings
{
    private const string Key = "settings.shake-to-scan";

    // Only an explicit "off" is stored; a device that never touched the switch has the feature on
    public bool IsEnabled
    {
        get => store.Get(Key) != bool.FalseString;
        set
        {
            if (value)
            {
                store.Remove(Key);
            }
            else
            {
                store.Set(Key, bool.FalseString);
            }
        }
    }
}
