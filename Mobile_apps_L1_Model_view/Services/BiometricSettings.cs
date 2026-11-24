namespace Mobile_apps_L1_Model_view.Services;

public sealed class BiometricSettings(IPreferencesStore store) : IBiometricSettings
{
    private const string Key = "settings.biometric-unlock";

    public bool IsEnabled
    {
        get => store.Get(Key) == bool.TrueString;
        set
        {
            if (value)
            {
                store.Set(Key, bool.TrueString);
            }
            else
            {
                store.Remove(Key);
            }
        }
    }
}
