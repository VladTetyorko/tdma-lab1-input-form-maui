using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

public interface IProductCharacteristicRepository
{
    Task<IReadOnlyList<ProductCharacteristic>> GetAllAsync();

    Task<IReadOnlyList<ProductCharacteristic>> GetByProductAsync(Guid productId);

    Task AddAsync(ProductCharacteristic value);
}
