using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

public interface ICharacteristicRepository
{
    Task<IReadOnlyList<Characteristic>> GetAllAsync();

    Task AddAsync(Characteristic characteristic);
}
