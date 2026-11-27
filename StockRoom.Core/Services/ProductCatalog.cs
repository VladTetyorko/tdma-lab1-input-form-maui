using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class ProductCatalog(
    IProductRepository products,
    ICharacteristicRepository characteristics,
    IProductCharacteristicRepository values,
    IOperationRepository operations,
    IUserRepository users,
    IStockService stock) : IProductCatalog
{
    public async Task<IReadOnlyList<ProductSummary>> GetSummariesAsync()
    {
        var quantities = await stock.GetQuantitiesAsync();
        var valuesByProduct = (await values.GetAllAsync()).ToLookup(v => v.ProductId);

        return (await products.GetAllAsync())
            .Select(p => Summarize(p, quantities.GetValueOrDefault(p.Id), valuesByProduct[p.Id], quantities.ContainsKey(p.Id)))
            .ToList();
    }

    public async Task<ProductSummary?> GetSummaryAsync(Guid productId)
    {
        var product = await products.GetByIdAsync(productId);
        if (product is null)
        {
            return null;
        }

        return Summarize(
            product,
            await stock.GetQuantityOnHandAsync(productId),
            await values.GetByProductAsync(productId),
            await operations.AnyForProductAsync(productId));
    }

    public async Task<ProductSummary?> GetSummaryByBarcodeAsync(string barcode) =>
        await products.GetByBarcodeAsync(barcode) is { } product ? await GetSummaryAsync(product.Id) : null;

    public async Task<ProductDetails?> GetDetailsAsync(Guid productId)
    {
        var product = await products.GetByIdAsync(productId);
        if (product is null)
        {
            return null;
        }

        var productValues = await values.GetByProductAsync(productId);
        var history = await BuildHistoryAsync(productId);
        var summary = Summarize(product, await stock.GetQuantityOnHandAsync(productId), productValues, history.Count > 0);

        var items = (await characteristics.GetAllAsync())
            .Select(c => new CharacteristicValueItem(
                c.Id, c.Name, c.ValueType, summary.Values.GetValueOrDefault(c.Id, string.Empty)))
            .ToList();

        return new ProductDetails(summary, items, history);
    }

    public async Task<IReadOnlyList<LocatedOperation>> GetLocatedOperationsAsync()
    {
        var names = (await users.GetAllAsync()).ToDictionary(u => u.Id, u => u.FullName);
        var productsById = (await products.GetAllAsync()).ToDictionary(p => p.Id);

        return (await operations.GetAllAsync())
            .Where(o => o.Location is not null && productsById.ContainsKey(o.ProductId))
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Select(o => new LocatedOperation(
                o.Id,
                productsById[o.ProductId].Name,
                productsById[o.ProductId].Barcode,
                o.Direction,
                o.Quantity,
                o.CreatedAt,
                names.GetValueOrDefault(o.CreatedByUserId, "Unknown"),
                o.Location!))
            .ToList();
    }

    private async Task<IReadOnlyList<OperationHistoryItem>> BuildHistoryAsync(Guid productId)
    {
        var names = (await users.GetAllAsync()).ToDictionary(u => u.Id, u => u.FullName);

        // Ids are UUID v7, so they break ties between operations recorded at the same instant
        return (await operations.GetByProductAsync(productId))
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Select(o => new OperationHistoryItem(
                o.CreatedAt,
                o.Direction,
                o.Quantity,
                names.GetValueOrDefault(o.CreatedByUserId, "Unknown"),
                o.Counterparty,
                o.Reason,
                o.Location))
            .ToList();
    }

    private static ProductSummary Summarize(
        Product product, int quantity, IEnumerable<ProductCharacteristic> productValues, bool hasOperations) =>
        new(product.Id, product.Name, product.Barcode, quantity)
        {
            PhotoPath = product.PhotoPath,
            Values = productValues.ToDictionary(v => v.CharacteristicId, v => v.Value),
            HasOperations = hasOperations
        };
}
