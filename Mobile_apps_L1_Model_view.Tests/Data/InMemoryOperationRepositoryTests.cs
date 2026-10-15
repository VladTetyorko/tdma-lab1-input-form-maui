using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Tests.Data;

public class InMemoryOperationRepositoryTests
{
    private readonly InMemoryOperationRepository _repository = new();

    [Fact]
    public async Task GetAllAsync_IsEmptyForNewRepository()
    {
        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task AddAsync_StoresOperationReturnedByGetAll()
    {
        var operation = new Operation { ProductId = Guid.NewGuid(), Direction = OperationDirection.In, Quantity = 5 };

        await _repository.AddAsync(operation);

        Assert.Same(operation, Assert.Single(await _repository.GetAllAsync()));
    }

    [Fact]
    public async Task GetByProductAsync_ReturnsOnlyOperationsOfThatProduct()
    {
        var drillId = Guid.NewGuid();
        var sawId = Guid.NewGuid();
        var drillIn = new Operation { ProductId = drillId, Direction = OperationDirection.In, Quantity = 10 };
        var drillOut = new Operation { ProductId = drillId, Direction = OperationDirection.Out, Quantity = 3 };
        var sawIn = new Operation { ProductId = sawId, Direction = OperationDirection.In, Quantity = 7 };
        await _repository.AddAsync(drillIn);
        await _repository.AddAsync(drillOut);
        await _repository.AddAsync(sawIn);

        var operations = await _repository.GetByProductAsync(drillId);

        Assert.Equal([drillIn.Id, drillOut.Id], operations.Select(o => o.Id).ToArray());
    }

    [Fact]
    public async Task GetByProductAsync_IsEmptyForProductWithoutOperations()
    {
        await _repository.AddAsync(new Operation { ProductId = Guid.NewGuid(), Direction = OperationDirection.In, Quantity = 1 });

        Assert.Empty(await _repository.GetByProductAsync(Guid.NewGuid()));
    }
}
