using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public interface IStockDocumentService
{
    // Records the whole receipt or issue as one document with one operation per product, or nothing at all.
    // The caller keeps the draft on failure and clears it on success
    Task<Result<StockDocument>> CommitAsync(StockDocumentDraft draft);

    // Several documents in one transaction (a stock count: a receipt and an issue): all of them or nothing
    Task<Result<IReadOnlyList<StockDocument>>> CommitManyAsync(IReadOnlyList<StockDocumentDraft> drafts);

    // The last three suppliers or recipients, for the chips of the review. Empty for a role that may not record this
    // direction; a role that may see only its own operations gets only its own names. Never log the result
    Task<IReadOnlyList<string>> GetRecentCounterpartiesAsync(OperationDirection direction);
}
