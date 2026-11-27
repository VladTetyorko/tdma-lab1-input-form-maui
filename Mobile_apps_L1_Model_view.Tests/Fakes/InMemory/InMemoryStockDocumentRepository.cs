using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Keeps documents in a list and their lines in the given operation repository, so quantities follow
public sealed class InMemoryStockDocumentRepository(InMemoryOperationRepository operations) : IStockDocumentRepository
{
    private readonly List<StockDocument> _documents = [];

    // The next AddAsync throws this and saves nothing
    public Exception? FailWith { get; set; }

    public IReadOnlyList<StockDocument> Documents => _documents;

    public Task<StockDocument?> GetByIdAsync(Guid id) => Task.FromResult(_documents.Find(d => d.Id == id));

    public Task<IReadOnlyList<StockDocument>> GetRecentAsync(int count) =>
        Task.FromResult<IReadOnlyList<StockDocument>>([.. Enumerable.Reverse(_documents).Take(count)]);

    public Task<IReadOnlyList<string>> GetRecentCounterpartiesAsync(
        OperationDirection direction, int count, Guid? createdBy = null) =>
        Task.FromResult<IReadOnlyList<string>>([.. Enumerable.Reverse(_documents)
            .Where(d => d.Direction == direction && d.Counterparty is not null && (createdBy is null || d.CreatedByUserId == createdBy))
            .Select(d => d.Counterparty!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(count)]);

    public Task<int> CountByNumberPrefixAsync(string prefix) =>
        Task.FromResult(_documents.Count(d => d.Number.StartsWith(prefix, StringComparison.Ordinal)));

    public Task AddAsync(StockDocument document, IReadOnlyList<Operation> lines) => AddAsync([(document, lines)]);

    public async Task AddAsync(IReadOnlyList<(StockDocument Document, IReadOnlyList<Operation> Operations)> batch)
    {
        if (FailWith is { } failure)
        {
            throw failure;
        }

        foreach (var (document, lines) in batch)
        {
            _documents.Add(document);
            foreach (var line in lines)
            {
                await operations.AddAsync(line);
            }
        }
    }
}
