using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// About ten products and a small warehouse: the operations are read and narrowed in memory
public sealed class OperationFeed(
    IOperationRepository operations,
    IProductRepository products,
    IUserRepository users,
    IStockDocumentRepository documents,
    ICurrentUserService currentUser,
    ILogger<OperationFeed> logger) : IOperationFeed
{
    public async Task<IReadOnlyList<OperationEntry>> GetAsync(OperationFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (currentUser.CurrentUser is not { } user)
        {
            logger.LogWarning("Operations were asked for while nobody is signed in");
            return [];
        }

        var ownOnly = filter.MineOnly || !RolePermissions.CanViewAllOperations(user.Role);
        var all = filter.ProductId is { } productId
            ? await operations.GetByProductAsync(productId)
            : await operations.GetAllAsync();

        // Ids are UUID v7, so they break ties between operations recorded at the same instant
        var chosen = all
            .Where(o => (!ownOnly || o.CreatedByUserId == user.Id)
                        && (filter.Direction is null || o.Direction == filter.Direction)
                        && (filter.SinceUtc is null || o.CreatedAt >= filter.SinceUtc))
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Take(filter.Count)
            .ToList();

        var names = (await users.GetAllAsync()).ToDictionary(u => u.Id, u => u.FullName);
        var productsById = (await products.GetAllAsync()).ToDictionary(p => p.Id);
        var read = new Dictionary<Guid, StockDocument?>();
        var entries = new List<OperationEntry>(chosen.Count);
        foreach (var operation in chosen)
        {
            StockDocument? document = null;
            if (operation.DocumentId is { } id && !read.TryGetValue(id, out document))
            {
                read[id] = document = await documents.GetByIdAsync(id);
            }

            var product = productsById.GetValueOrDefault(operation.ProductId);
            entries.Add(new OperationEntry(
                operation.Id, operation.ProductId, product?.Name ?? "Unknown product", product?.Barcode ?? string.Empty,
                operation.Direction, operation.Quantity, operation.CreatedByUserId,
                names.GetValueOrDefault(operation.CreatedByUserId, "Unknown user"), operation.CreatedAt)
            {
                DocumentNumber = document?.Number,
                Counterparty = document?.Counterparty ?? operation.Counterparty,
                Note = document?.Note ?? operation.Reason,
                Location = operation.Location
            });
        }

        return entries;
    }
}
