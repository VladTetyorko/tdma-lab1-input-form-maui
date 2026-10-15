using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class ProductService(
    IProductRepository products,
    IOperationRepository operations,
    IProductValidator validator,
    ICurrentUserService currentUser,
    IClock clock) : IProductService
{
    public const string HasOperationsMessage =
        "This product has stock history, so it cannot be deleted: operations are permanent records " +
        "and must keep pointing at their product.";

    public async Task<Result<Product>> AddAsync(string? name, string? barcode)
    {
        var denied = CheckManager("adding", "add");
        if (denied is not null)
        {
            return Result<Product>.Failure(denied);
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
            CreatedByUserId = currentUser.CurrentUser!.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        await products.AddAsync(product);
        return Result<Product>.Success(product);
    }

    public async Task<Result<Product>> UpdateAsync(Guid id, string? name, string? barcode)
    {
        var denied = CheckManager("editing", "edit");
        if (denied is not null)
        {
            return Result<Product>.Failure(denied);
        }

        var product = await products.GetByIdAsync(id);
        if (product is null)
        {
            return Result<Product>.Failure("Product not found.");
        }

        // The product's own barcode is not a duplicate of itself
        var error = validator.ValidateName(name) ?? await validator.ValidateBarcodeAsync(barcode, id);
        if (error is not null)
        {
            return Result<Product>.Failure(error);
        }

        product.Name = name!.Trim();
        product.Barcode = barcode!.Trim();
        product.UpdatedAt = clock.UtcNow;
        await products.UpdateAsync(product);
        return Result<Product>.Success(product);
    }

    public async Task<Result<Product>> DeleteAsync(Guid id)
    {
        var denied = CheckManager("deleting", "delete");
        if (denied is not null)
        {
            return Result<Product>.Failure(denied);
        }

        var product = await products.GetByIdAsync(id);
        if (product is null)
        {
            return Result<Product>.Failure("Product not found.");
        }

        if (await operations.AnyForProductAsync(id))
        {
            return Result<Product>.Failure(HasOperationsMessage);
        }

        await products.DeleteAsync(id);
        return Result<Product>.Success(product);
    }

    // Null when the current user may manage products; otherwise the message for the user
    private string? CheckManager(string gerund, string verb)
    {
        var user = currentUser.CurrentUser;
        if (user is null)
        {
            return $"Sign in before {gerund} products.";
        }

        return RolePermissions.CanManageProducts(user.Role) ? null : $"Your role ({user.Role}) cannot {verb} products.";
    }
}
