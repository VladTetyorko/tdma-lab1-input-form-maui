using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public interface IStockService
{
    // Sum of In minus sum of Out for one product
    Task<int> GetQuantityOnHandAsync(Guid productId);

    // Quantity on hand for every product that has operations; the others are absent (zero)
    Task<IReadOnlyDictionary<Guid, int>> GetQuantitiesAsync();

    // Records "quantity units of the product moved In/Out" as the current user
    Task<Result<Operation>> RecordAsync(Guid productId, OperationDirection direction, int quantity,
        string? counterparty = null, string? reason = null);
}
