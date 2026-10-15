using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync();

    Task<Product?> GetByIdAsync(Guid id);

    Task<Product?> GetByBarcodeAsync(string barcode);

    Task AddAsync(Product product);
}
