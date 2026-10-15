using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

public sealed class InMemoryUserRepository : InMemoryRepository<User>, IUserRepository
{
    public Task<IReadOnlyList<User>> GetAllAsync() => QueryAsync();

    public Task<User?> GetByIdAsync(Guid id) => FindAsync(u => u.Id == id);

    public Task AddAsync(User item) => InsertAsync(item);
}
