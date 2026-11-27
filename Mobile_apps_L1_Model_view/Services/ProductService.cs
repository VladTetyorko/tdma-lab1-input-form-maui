using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class ProductService(
    IProductRepository products,
    IOperationRepository operations,
    IProductValidator validator,
    ICurrentUserService currentUser,
    IClock clock,
    ILogger<ProductService> logger) : IProductService
{
    public const string HasOperationsMessage =
        "This product has stock history, so it cannot be deleted: operations are permanent records " +
        "and must keep pointing at their product.";

    public const string InvalidPhotoMessage = "The photo path is not valid.";

    public async Task<Result<Product>> AddAsync(string? name, string? barcode, string? photoPath = null)
    {
        var denied = CheckManager("adding", "add");
        if (denied is not null)
        {
            logger.LogWarning("Adding a product denied: {Reason}", denied);
            return Result<Product>.Failure(denied);
        }

        var error = validator.ValidateName(name) ?? await validator.ValidateBarcodeAsync(barcode) ?? ValidatePhoto(photoPath);
        if (error is not null)
        {
            logger.LogWarning("Adding a product rejected: {Reason}", error);
            return Result<Product>.Failure(error);
        }

        var now = clock.UtcNow;
        var product = new Product
        {
            Name = name!.Trim(),
            Barcode = barcode!.Trim(),
            PhotoPath = photoPath,
            CreatedByUserId = currentUser.CurrentUser!.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        await products.AddAsync(product);
        logger.LogInformation("Added product {ProductId} with barcode {Barcode}", product.Id, product.Barcode);
        return Result<Product>.Success(product);
    }

    public Task<Result<Product>> UpdateAsync(Guid id, string? name, string? barcode) =>
        UpdateCoreAsync(id, name, barcode, keepPhoto: true, photoPath: null);

    public Task<Result<Product>> UpdateAsync(Guid id, string? name, string? barcode, string? photoPath) =>
        UpdateCoreAsync(id, name, barcode, keepPhoto: false, photoPath);

    public async Task<Result<Product>> SetPhotoAsync(Guid id, string? photoPath)
    {
        var denied = CheckManager("changing the photo of", "change the photo of");
        if (denied is not null)
        {
            logger.LogWarning("Changing the photo of product {ProductId} denied: {Reason}", id, denied);
            return Result<Product>.Failure(denied);
        }

        var product = await products.GetByIdAsync(id);
        if (product is null)
        {
            logger.LogWarning("Changing the photo rejected: product {ProductId} not found", id);
            return Result<Product>.Failure("Product not found.");
        }

        var error = ValidatePhoto(photoPath);
        if (error is not null)
        {
            logger.LogWarning("Changing the photo of product {ProductId} rejected: {Reason}", id, error);
            return Result<Product>.Failure(error);
        }

        product.PhotoPath = photoPath;
        product.UpdatedAt = clock.UtcNow;
        await products.UpdateAsync(product);
        logger.LogInformation("Product {ProductId} photo {State}", id, photoPath is null ? "removed" : "set");
        return Result<Product>.Success(product);
    }

    private async Task<Result<Product>> UpdateCoreAsync(Guid id, string? name, string? barcode, bool keepPhoto, string? photoPath)
    {
        var denied = CheckManager("editing", "edit");
        if (denied is not null)
        {
            logger.LogWarning("Editing product {ProductId} denied: {Reason}", id, denied);
            return Result<Product>.Failure(denied);
        }

        var product = await products.GetByIdAsync(id);
        if (product is null)
        {
            logger.LogWarning("Editing rejected: product {ProductId} not found", id);
            return Result<Product>.Failure("Product not found.");
        }

        // The product's own barcode is not a duplicate of itself
        var error = validator.ValidateName(name) ?? await validator.ValidateBarcodeAsync(barcode, id)
            ?? (keepPhoto ? null : ValidatePhoto(photoPath));
        if (error is not null)
        {
            logger.LogWarning("Editing product {ProductId} rejected: {Reason}", id, error);
            return Result<Product>.Failure(error);
        }

        product.Name = name!.Trim();
        product.Barcode = barcode!.Trim();
        if (!keepPhoto)
        {
            product.PhotoPath = photoPath;
        }

        product.UpdatedAt = clock.UtcNow;
        await products.UpdateAsync(product);
        logger.LogInformation("Updated product {ProductId}", product.Id);
        return Result<Product>.Success(product);
    }

    public async Task<Result<Product>> DeleteAsync(Guid id)
    {
        var denied = CheckManager("deleting", "delete");
        if (denied is not null)
        {
            logger.LogWarning("Deleting product {ProductId} denied: {Reason}", id, denied);
            return Result<Product>.Failure(denied);
        }

        var product = await products.GetByIdAsync(id);
        if (product is null)
        {
            logger.LogWarning("Deleting rejected: product {ProductId} not found", id);
            return Result<Product>.Failure("Product not found.");
        }

        if (await operations.AnyForProductAsync(id))
        {
            logger.LogWarning("Deleting product {ProductId} rejected: it has stock history", id);
            return Result<Product>.Failure(HasOperationsMessage);
        }

        await products.DeleteAsync(id);
        logger.LogInformation("Deleted product {ProductId}", id);
        return Result<Product>.Success(product);
    }

    // A photo is optional; when there is one it must be a relative path inside the photos folder
    private static string? ValidatePhoto(string? photoPath) =>
        photoPath is null || PhotoPaths.IsValidRelative(photoPath) ? null : InvalidPhotoMessage;

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
