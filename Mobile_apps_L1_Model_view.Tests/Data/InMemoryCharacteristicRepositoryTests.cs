using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Tests.Data;

public class InMemoryCharacteristicRepositoryTests
{
    private readonly InMemoryCharacteristicRepository _repository = new();

    [Fact]
    public async Task GetAllAsync_IsEmptyForNewRepository()
    {
        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task AddAsync_StoresCharacteristicReturnedByGetAll()
    {
        var weight = new Characteristic { Name = "Weight (kg)", ValueType = CharacteristicValueType.Number };

        await _repository.AddAsync(weight);

        Assert.Same(weight, Assert.Single(await _repository.GetAllAsync()));
    }

    [Fact]
    public async Task AddAsync_RejectsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddAsync(null!));
    }
}
