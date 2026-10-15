using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

// Operations are immutable: there is no update or delete
public interface IOperationRepository
{
    Task<IReadOnlyList<Operation>> GetAllAsync();

    Task<IReadOnlyList<Operation>> GetByProductAsync(Guid productId);

    Task AddAsync(Operation operation);
}
