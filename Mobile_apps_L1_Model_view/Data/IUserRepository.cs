using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllAsync();

    Task AddAsync(User user);
}
