using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class StockService(
    IOperationRepository operations,
    IProductRepository products,
    ICurrentUserService currentUser,
    IClock clock,
    ILogger<StockService> logger) : IStockService
{
    public async Task<int> GetQuantityOnHandAsync(Guid productId) =>
        Sum(await operations.GetByProductAsync(productId));

    public async Task<IReadOnlyDictionary<Guid, int>> GetQuantitiesAsync() =>
        (await operations.GetAllAsync())
        .GroupBy(o => o.ProductId)
        .ToDictionary(g => g.Key, Sum);

    public async Task<Result<Operation>> RecordAsync(Guid productId, OperationDirection direction, int quantity,
        string? counterparty = null, string? reason = null)
    {
        var user = currentUser.CurrentUser;
        if (user is null)
        {
            logger.LogWarning("Operation on product {ProductId} refused: nobody is signed in", productId);
            return Result<Operation>.Failure("Sign in before recording operations.");
        }

        if (!RolePermissions.CanRecord(user.Role, direction))
        {
            logger.LogWarning("Role {Role} (user {UserId}) denied recording {Direction} on product {ProductId}",
                user.Role, user.Id, direction, productId);
            return Result<Operation>.Failure($"Your role ({user.Role}) cannot record {direction} operations.");
        }

        if (quantity <= 0)
        {
            logger.LogWarning("Operation on product {ProductId} rejected: quantity {Quantity} is not positive", productId, quantity);
            return Result<Operation>.Failure("Quantity must be greater than zero.");
        }

        if (await products.GetByIdAsync(productId) is null)
        {
            logger.LogWarning("Operation rejected: product {ProductId} not found", productId);
            return Result<Operation>.Failure("Product not found.");
        }

        if (direction == OperationDirection.Out && quantity > await GetQuantityOnHandAsync(productId))
        {
            logger.LogWarning("Out of {Quantity} units of product {ProductId} rejected: not enough on hand", quantity, productId);
            return Result<Operation>.Failure("Not enough units on hand.");
        }

        var now = clock.UtcNow;
        var operation = new Operation
        {
            ProductId = productId,
            Direction = direction,
            Quantity = quantity,
            Counterparty = counterparty,
            Reason = reason,
            CreatedByUserId = user.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        try
        {
            await operations.AddAsync(operation);
        }
        catch (EncryptionException ex)
        {
            // The counterparty is personal data and is never stored unprotected (and never logged)
            logger.LogError(ex, "Operation on product {ProductId} was not saved: the field could not be encrypted", productId);
            return Result<Operation>.Failure($"The operation was not saved. {ex.Message}");
        }

        logger.LogInformation("Recorded {Direction} of {Quantity} units of product {ProductId} as operation {OperationId} by user {UserId}",
            direction, quantity, productId, operation.Id, user.Id);
        return Result<Operation>.Success(operation);
    }

    // The one place where quantity on hand is calculated
    private static int Sum(IEnumerable<Operation> productOperations) =>
        productOperations.Sum(o => o.Direction == OperationDirection.In ? o.Quantity : -o.Quantity);
}
