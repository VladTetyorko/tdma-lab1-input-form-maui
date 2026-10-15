using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

// Keeps OperationDraft in IAppStateService (which stays generic) with the counterparty encrypted:
// the stored record holds the cipher text in its Counterparty field.
public sealed class OperationDraftStore(
    IAppStateService appState,
    IFieldEncryptor encryptor,
    ILogger<OperationDraftStore> logger) : IOperationDraftStore
{
    // Encryption is async, so saves can finish out of order; only the newest one may write
    private int _version;

    public async Task SaveAsync(OperationDraft draft)
    {
        var version = Interlocked.Increment(ref _version);
        var counterparty = await TryEncryptAsync(draft.Counterparty);
        if (version == Volatile.Read(ref _version))
        {
            appState.SetDraft(draft with { Counterparty = counterparty });
        }
    }

    public async Task<OperationDraft?> LoadAsync(string barcode)
    {
        if (appState.GetDraft<OperationDraft>() is not { } stored || stored.Barcode != barcode)
        {
            return null;
        }

        return stored with { Counterparty = await TryDecryptAsync(stored.Counterparty) };
    }

    public void Clear()
    {
        Interlocked.Increment(ref _version);
        appState.ClearDraft<OperationDraft>();
    }

    private async Task<string> TryEncryptAsync(string plainText)
    {
        if (plainText.Length == 0)
        {
            return plainText;
        }

        try
        {
            return await encryptor.EncryptAsync(plainText);
        }
        catch (EncryptionException ex)
        {
            logger.LogWarning(ex, "Counterparty of the operation draft cannot be encrypted and is not stored");
            return string.Empty;
        }
    }

    private async Task<string> TryDecryptAsync(string cipherText)
    {
        if (cipherText.Length == 0)
        {
            return cipherText;
        }

        try
        {
            return await encryptor.DecryptAsync(cipherText);
        }
        catch (EncryptionException ex)
        {
            logger.LogWarning(ex, "Counterparty of the operation draft cannot be decrypted and is dropped");
            return string.Empty;
        }
    }
}
