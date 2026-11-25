using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public interface IStockDocumentService
{
    // Records the whole receipt or issue as one document with one operation per product, or nothing at all.
    // The caller keeps the draft on failure and clears it on success
    Task<Result<StockDocument>> CommitAsync(StockDocumentDraft draft);
}
