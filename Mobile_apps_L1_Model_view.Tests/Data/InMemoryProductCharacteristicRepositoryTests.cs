using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Tests.Fakes;
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

    [Fact]
    public async Task UpdateAsync_ReplacesTheStoredRowWithTheSameId()
    {
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var characteristicId = Guid.NewGuid();
        await _repository.AddAsync(new ProductCharacteristic
        {
            Id = id, ProductId = productId, CharacteristicId = characteristicId, Value = "1.5"
        });
        var replacement = new ProductCharacteristic
        {
            Id = id, ProductId = productId, CharacteristicId = characteristicId, Value = "2"
        };

        await _repository.UpdateAsync(replacement);

        var stored = Assert.Single(await _repository.GetByProductAsync(productId));
        Assert.Same(replacement, stored);
        Assert.Equal("2", stored.Value);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.UpdateAsync(new ProductCharacteristic
        {
            ProductId = Guid.NewGuid(),
            CharacteristicId = Guid.NewGuid(),
            Value = "2"
        }));

        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyTheGivenValue()
    {
        var kept = new ProductCharacteristic { ProductId = Guid.NewGuid(), CharacteristicId = Guid.NewGuid(), Value = "A" };
        var removed = new ProductCharacteristic { ProductId = Guid.NewGuid(), CharacteristicId = Guid.NewGuid(), Value = "B" };
        await _repository.AddAsync(kept);
        await _repository.AddAsync(removed);

        await _repository.DeleteAsync(removed.Id);

        Assert.Same(kept, Assert.Single(await _repository.GetAllAsync()));
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_IsNoOp()
    {
        var kept = new ProductCharacteristic { ProductId = Guid.NewGuid(), CharacteristicId = Guid.NewGuid(), Value = "A" };
        await _repository.AddAsync(kept);

        await _repository.DeleteAsync(Guid.NewGuid());

        Assert.Same(kept, Assert.Single(await _repository.GetAllAsync()));
    }
}
