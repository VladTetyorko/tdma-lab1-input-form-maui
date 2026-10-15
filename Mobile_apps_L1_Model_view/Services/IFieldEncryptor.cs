namespace Mobile_apps_L1_Model_view.Services;

// Encrypts single text fields before they are stored (personal data such as Operation.Counterparty).
// The result is plain text (base64), so it fits an ordinary TEXT column and survives a database export.
public interface IFieldEncryptor
{
    // Creates the key on first use. Throws EncryptionException when the key cannot be read or saved
    Task<string> EncryptAsync(string plainText);

    // Never creates a key. Throws EncryptionException when the key is missing, is a different key
    // than the one used to encrypt, or the text was changed
    Task<string> DecryptAsync(string cipherText);
}
