using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Stores both drafts as one record in IAppStateService, which stays generic; sign-out clears it with everything else
public sealed class StockDocumentDraftStore(
    IAppStateService appState,
    IFieldEncryptor encryptor,
    ILogger<StockDocumentDraftStore> logger) : IStockDocumentDraftStore
{
    // Encryption is async, so saves can finish out of order; only the newest one per direction may write
    private readonly int[] _versions = new int[2];

    public event EventHandler? Changed;

    public async Task SaveAsync(StockDocumentDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var direction = draft.Direction;
        var version = ++_versions[(int)direction];
        var lines = draft.Lines.ToList();
        var note = draft.Note;
        var isEmpty = draft.IsEmpty;
        var counterparty = await TryEncryptAsync(draft.Counterparty);
        if (version == _versions[(int)direction])
        {
            Write(direction, isEmpty ? null : new Stored(counterparty, note, lines));
        }
    }

    public async Task<StockDocumentDraft> LoadAsync(OperationDirection direction)
    {
        var draft = new StockDocumentDraft(direction);
        if (Read(direction) is not { } stored)
        {
            return draft;
        }

        // Added from the last line to the first, because every added line goes to the top
        foreach (var line in stored.Lines.Reverse().Where(l => !string.IsNullOrWhiteSpace(l.Barcode) && l.Quantity > 0))
        {
            draft.Add(line.Barcode, line.Quantity);
        }

        draft.Note = stored.Note;
        draft.Counterparty = await TryDecryptAsync(stored.Counterparty);
        return draft;
    }

    public void Clear(OperationDirection direction)
    {
        _versions[(int)direction]++;
        Write(direction, null);
    }

    public int LineCount(OperationDirection direction) => Read(direction)?.Lines.Count ?? 0;

    private Stored? Read(OperationDirection direction)
    {
        var all = appState.GetDraft<StoredDocuments>();
        return direction == OperationDirection.In ? all?.Receive : all?.Issue;
    }

    private void Write(OperationDirection direction, Stored? stored)
    {
        var all = appState.GetDraft<StoredDocuments>() ?? new StoredDocuments(null, null);
        all = direction == OperationDirection.In ? all with { Receive = stored } : all with { Issue = stored };
        if (all.Receive is null && all.Issue is null)
        {
            appState.ClearDraft<StoredDocuments>();
        }
        else
        {
            appState.SetDraft(all);
        }

        Changed?.Invoke(this, EventArgs.Empty);
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
            logger.LogWarning(ex, "Counterparty of a document draft cannot be encrypted and is not stored");
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
            logger.LogWarning(ex, "Counterparty of a document draft cannot be decrypted and is dropped");
            return string.Empty;
        }
    }

    public sealed record Stored(string Counterparty, string Note, IReadOnlyList<DraftLine> Lines);

    public sealed record StoredDocuments(Stored? Receive, Stored? Issue);
}
