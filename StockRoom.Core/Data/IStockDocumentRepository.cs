using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

// Documents are immutable: there is no update or delete
public interface IStockDocumentRepository
{
    Task<StockDocument?> GetByIdAsync(Guid id);

    // Newest first, at most "count"
    Task<IReadOnlyList<StockDocument>> GetRecentAsync(int count);

    // The different suppliers (In) or recipients (Out) of the newest documents, newest first, decrypted; only those of
    // createdBy when given. Unreadable values are left out. Personal data: never log the result
    Task<IReadOnlyList<string>> GetRecentCounterpartiesAsync(OperationDirection direction, int count, Guid? createdBy = null);

    // How many numbers start with the prefix, e.g. "R-20261124-" (for the next number of a day)
    Task<int> CountByNumberPrefixAsync(string prefix);

    // Saves the document and all its operations, or nothing at all. Every operation must point to the document
    Task AddAsync(StockDocument document, IReadOnlyList<Operation> operations);

    // The same for several documents in one transaction
    Task AddAsync(IReadOnlyList<(StockDocument Document, IReadOnlyList<Operation> Operations)> batch);
}
