using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class InMemoryProductRepository : InMemoryRepository<Product>, IProductRepository
{
    public Task<IReadOnlyList<Product>> GetAllAsync() => QueryAsync();

    public Task<Product?> GetByIdAsync(Guid id) => FindAsync(p => p.Id == id);

    public Task<Product?> GetByBarcodeAsync(string barcode) => FindAsync(p => p.Barcode == barcode);

    public Task AddAsync(Product product) => InsertAsync(product);

    public Task UpdateAsync(Product product) => ReplaceAsync(product);

    public Task DeleteAsync(Guid id) => RemoveAsync(id);
}
