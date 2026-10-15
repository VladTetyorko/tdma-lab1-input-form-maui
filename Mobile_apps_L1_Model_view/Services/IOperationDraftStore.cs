namespace Mobile_apps_L1_Model_view.Services;

// The unsaved operation form kept across app restarts. The counterparty is personal data, so it is
// stored encrypted (IFieldEncryptor, the same mechanism as in the database); callers only see plain text.
public interface IOperationDraftStore
{
    // Never throws for encryption problems: when the counterparty cannot be encrypted the draft is
    // stored without it (never in clear text). A newer call or Clear() wins over an older, slower one
    Task SaveAsync(OperationDraft draft);

    // The draft of this product, or null. When the counterparty cannot be decrypted (key lost or
    // changed, text tampered) the draft is returned with an empty counterparty
    Task<OperationDraft?> LoadAsync(string barcode);

    void Clear();
}
