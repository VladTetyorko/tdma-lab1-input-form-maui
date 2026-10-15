using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Tests.Data;

public class InMemoryProductCharacteristicRepositoryTests
{
    private readonly InMemoryProductCharacteristicRepository _repository = new();

    [Fact]
    public async Task GetAllAsync_IsEmptyForNewRepository()
    {
        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task GetByProductAsync_ReturnsOnlyValuesOfThatProduct()
    {
        var drillId = Guid.NewGuid();
        var sawId = Guid.NewGuid();
        var weight = Guid.NewGuid();
        var drillWeight = new ProductCharacteristic { ProductId = drillId, CharacteristicId = weight, Value = "1.6" };
        var sawWeight = new ProductCharacteristic { ProductId = sawId, CharacteristicId = weight, Value = "0.9" };
        await _repository.AddAsync(drillWeight);
        await _repository.AddAsync(sawWeight);

        var values = await _repository.GetByProductAsync(drillId);

        Assert.Same(drillWeight, Assert.Single(values));
    }

    [Fact]
    public async Task GetByProductAsync_IsEmptyForProductWithoutValues()
    {
        await _repository.AddAsync(new ProductCharacteristic
        {
            ProductId = Guid.NewGuid(),
            CharacteristicId = Guid.NewGuid(),
            Value = "Blue"
        });

        Assert.Empty(await _repository.GetByProductAsync(Guid.NewGuid()));
    }
}
