namespace Mobile_apps_L1_Model_view.Services;

// Android Keystore, iOS Keychain, Windows data protection (through MAUI SecureStorage)
public sealed class MauiSecretStore : ISecretStore
{
    public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);

    public Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);
}
