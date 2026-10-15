using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public interface IProductService
{
    // Manager only; validates name and barcode; the current user becomes the owner
    Task<Result<Product>> AddAsync(string? name, string? barcode);
}
