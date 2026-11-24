using System.Security.Cryptography;

namespace Mobile_apps_L1_Model_view.Services;

// AES-256-CBC with an HMAC-SHA256 over the result (encrypt-then-MAC), a fresh random IV for every value.
// Stored text: base64( version(1) | IV(16) | ciphertext | HMAC(32) ).
// The 64-byte master key (32 for AES, 32 for HMAC) is generated once and kept in ISecretStore.
// CBC+HMAC instead of AES-GCM because it runs on every target platform of the app.
public sealed class AesFieldEncryptor(ISecretStore secrets) : IFieldEncryptor
{
    public const string KeyName = "stockroom.field-key.v1";

    private const byte Version = 1;
    private const int KeyLength = 32;
    private const int IvLength = 16;
    private const int MacLength = 32;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private byte[]? _key;

    public async Task<string> EncryptAsync(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);
        var key = await GetKeyAsync(createIfMissing: true);

        using var aes = Aes.Create();
        aes.Key = key[..KeyLength];
        var iv = RandomNumberGenerator.GetBytes(IvLength);
        var encrypted = aes.EncryptCbc(System.Text.Encoding.UTF8.GetBytes(plainText), iv, PaddingMode.PKCS7);

        var message = new byte[1 + IvLength + encrypted.Length + MacLength];
        message[0] = Version;
        iv.CopyTo(message, 1);
        encrypted.CopyTo(message, 1 + IvLength);
        var macOffset = message.Length - MacLength;
        HMACSHA256.HashData(key.AsSpan(KeyLength), message.AsSpan(0, macOffset)).CopyTo(message, macOffset);
        return Convert.ToBase64String(message);
    }

    public async Task<string> DecryptAsync(string cipherText)
    {
        ArgumentNullException.ThrowIfNull(cipherText);
        var key = await GetKeyAsync(createIfMissing: false);

        byte[] message;
        try
        {
            message = Convert.FromBase64String(cipherText);
        }
        catch (FormatException ex)
        {
            throw new EncryptionException("The stored value is not valid encrypted data.", ex);
        }

        if (message.Length < 1 + IvLength + MacLength || message[0] != Version)
        {
            throw new EncryptionException("The stored value is not valid encrypted data.");
        }

        var macOffset = message.Length - MacLength;
        var expected = HMACSHA256.HashData(key.AsSpan(KeyLength), message.AsSpan(0, macOffset));
        if (!CryptographicOperations.FixedTimeEquals(expected, message.AsSpan(macOffset)))
        {
            throw new EncryptionException(
                "The value cannot be decrypted: it was encrypted with a different key or was changed.");
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = key[..KeyLength];
            var plain = aes.DecryptCbc(
                message.AsSpan(1 + IvLength, macOffset - 1 - IvLength), message.AsSpan(1, IvLength), PaddingMode.PKCS7);
            return System.Text.Encoding.UTF8.GetString(plain);
        }
        catch (CryptographicException ex)
        {
            throw new EncryptionException("The value cannot be decrypted.", ex);
        }
    }

    private async Task<byte[]> GetKeyAsync(bool createIfMissing)
    {
        if (_key is not null)
        {
            return _key;
        }

        await _gate.WaitAsync();
        try
        {
            return _key ??= await LoadOrCreateKeyAsync(createIfMissing);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<byte[]> LoadOrCreateKeyAsync(bool createIfMissing)
    {
        try
        {
            if (await secrets.GetAsync(KeyName) is { } stored)
            {
                return Convert.FromBase64String(stored);
            }

            if (!createIfMissing)
            {
                throw new EncryptionException(
                    "The encryption key is missing from secure storage, so protected data cannot be read.");
            }

            var key = RandomNumberGenerator.GetBytes(KeyLength * 2);
            await secrets.SetAsync(KeyName, Convert.ToBase64String(key));
            return key;
        }
        catch (EncryptionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new EncryptionException("Secure storage is not available, so protected data cannot be used.", ex);
        }
    }
}
