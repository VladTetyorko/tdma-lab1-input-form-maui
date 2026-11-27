using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Keeps one unfinished receipt and one unfinished issue across restarts, the counterparty encrypted
public interface IStockDocumentDraftStore
{
    // Raised after a draft was saved or cleared (the menu shows the number of lines)
    event EventHandler? Changed;

    Task SaveAsync(StockDocumentDraft draft);

    // An empty draft when nothing was saved
    Task<StockDocumentDraft> LoadAsync(OperationDirection direction);

    void Clear(OperationDirection direction);

    int LineCount(OperationDirection direction);
}
