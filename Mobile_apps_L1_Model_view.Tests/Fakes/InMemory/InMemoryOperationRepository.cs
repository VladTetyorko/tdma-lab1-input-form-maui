using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class InMemoryOperationRepository : InMemoryRepository<Operation>, IOperationRepository
{
    public Task<IReadOnlyList<Operation>> GetAllAsync() => QueryAsync();

    public Task<IReadOnlyList<Operation>> GetByProductAsync(Guid productId) =>
        QueryAsync(o => o.ProductId == productId);

    public async Task<bool> AnyForProductAsync(Guid productId) => (await GetByProductAsync(productId)).Count > 0;

    public Task AddAsync(Operation operation) => InsertAsync(operation);
}
