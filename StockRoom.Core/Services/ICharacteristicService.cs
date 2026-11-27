using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public interface ICharacteristicService
{
    Task<IReadOnlyList<Characteristic>> GetAllAsync();

    // Manager only; the name must be unique (ignoring case); the current user becomes the owner
    Task<Result<Characteristic>> AddAsync(string? name, CharacteristicValueType valueType);

    // Manager only. Sets, changes or (for an empty value) removes the product's value for each
    // listed characteristic. All values are validated first: one bad value changes nothing.
    // Returns how many stored values were created, changed or removed.
    Task<Result<int>> SetValuesAsync(Guid productId, IReadOnlyDictionary<Guid, string?> values);
}
