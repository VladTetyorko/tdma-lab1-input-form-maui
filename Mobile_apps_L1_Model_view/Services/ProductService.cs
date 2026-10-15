using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class ProductService(
    IProductRepository products,
    IProductValidator validator,
    ICurrentUserService currentUser,
    IClock clock) : IProductService
{
    public async Task<Result<Product>> AddAsync(string? name, string? barcode)
    {
        var user = currentUser.CurrentUser;
        if (user is null)
        {
            return Result<Product>.Failure("Sign in before adding products.");
        }

        if (!RolePermissions.CanManageProducts(user.Role))
        {
            return Result<Product>.Failure($"Your role ({user.Role}) cannot add products.");
        }

        var error = validator.ValidateName(name) ?? await validator.ValidateBarcodeAsync(barcode);
        if (error is not null)
        {
            return Result<Product>.Failure(error);
        }

        var now = clock.UtcNow;
        var product = new Product
        {
            Name = name!.Trim(),
            Barcode = barcode!.Trim(),
            CreatedByUserId = user.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        await products.AddAsync(product);
        return Result<Product>.Success(product);
    }
}
