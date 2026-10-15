using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

public sealed class InMemoryProductCharacteristicRepository
    : InMemoryRepository<ProductCharacteristic>, IProductCharacteristicRepository
{
    public Task<IReadOnlyList<ProductCharacteristic>> GetAllAsync() => QueryAsync();

    public Task<IReadOnlyList<ProductCharacteristic>> GetByProductAsync(Guid productId) =>
        QueryAsync(v => v.ProductId == productId);

    public Task AddAsync(ProductCharacteristic value) => InsertAsync(value);

    public Task UpdateAsync(ProductCharacteristic value) => ReplaceAsync(value);

    public Task DeleteAsync(Guid id) => RemoveAsync(id);
}
