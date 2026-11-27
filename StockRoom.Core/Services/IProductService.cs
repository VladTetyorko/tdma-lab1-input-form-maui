using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Manager only. Every method validates, then writes through the repository
public interface IProductService
{
    // Validates name and barcode (format and unique); the current user becomes the owner.
    // photoPath is the relative path from IPhotoService (null = no photo)
    Task<Result<Product>> AddAsync(string? name, string? barcode, string? photoPath = null);

    // Changes name and barcode with the same validation; the barcode may stay as it is. The photo stays as it is
    Task<Result<Product>> UpdateAsync(Guid id, string? name, string? barcode);

    // Same, and sets the photo too (null removes it)
    Task<Result<Product>> UpdateAsync(Guid id, string? name, string? barcode, string? photoPath);

    // Sets or removes only the photo of a product (Manager)
    Task<Result<Product>> SetPhotoAsync(Guid id, string? photoPath);

    // Only a product without operations can be deleted (history must stay complete); its characteristic
    // values go with it. Returns the deleted product
    Task<Result<Product>> DeleteAsync(Guid id);
}
