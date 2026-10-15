using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

public sealed class InMemoryCharacteristicRepository : InMemoryRepository<Characteristic>, ICharacteristicRepository
{
    public Task<IReadOnlyList<Characteristic>> GetAllAsync() => QueryAsync();

    public Task AddAsync(Characteristic item) => InsertAsync(item);
}
