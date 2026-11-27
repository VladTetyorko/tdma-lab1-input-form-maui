namespace Mobile_apps_L1_Model_view.Services;

public sealed class ScanSoundSettings(IPreferencesStore store) : IScanSoundSettings
{
    private const string Key = "settings.scan-sound";

    // Only an explicit "off" is stored
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
