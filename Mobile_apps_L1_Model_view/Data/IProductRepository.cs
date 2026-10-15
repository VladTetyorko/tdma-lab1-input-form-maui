using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync();

    Task<Product?> GetByIdAsync(Guid id);

    Task<Product?> GetByBarcodeAsync(string barcode);

    Task AddAsync(Product product);

    // Replaces the stored row that has the same Id
    Task UpdateAsync(Product product);

    // Removes the product together with its characteristic values. Fails while operations still point at it
    // (the database refuses); ProductService checks that first and explains it to the user
    Task DeleteAsync(Guid id);
}
