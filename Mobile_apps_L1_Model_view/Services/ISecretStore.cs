namespace Mobile_apps_L1_Model_view.Services;

// Small secrets (the encryption key) in the platform's protected storage: SecureStorage in the app
public interface ISecretStore
{
    // Null when nothing is stored under the key
    Task<string?> GetAsync(string key);

    Task SetAsync(string key, string value);
}
