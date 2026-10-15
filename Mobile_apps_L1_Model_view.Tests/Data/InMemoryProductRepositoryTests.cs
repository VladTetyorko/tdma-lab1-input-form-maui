using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Tests.Data;

public class InMemoryProductRepositoryTests
{
    private readonly InMemoryProductRepository _repository = new();

    [Fact]
    public async Task GetAllAsync_IsEmptyForNewRepository()
    {
        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task AddAsync_StoresProductReturnedByGetAll()
    {
        var drill = new Product { Name = "Drill", Barcode = "4006381333931" };

        await _repository.AddAsync(drill);

        Assert.Same(drill, Assert.Single(await _repository.GetAllAsync()));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSnapshotThatIsNotAffectedByLaterAdds()
    {
        var snapshot = await _repository.GetAllAsync();
        await _repository.AddAsync(new Product { Name = "Drill", Barcode = "4006381333931" });

        Assert.Empty(snapshot);
        Assert.Single(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProductWithThatId()
    {
        var drill = new Product { Name = "Drill", Barcode = "4006381333931" };
        var saw = new Product { Name = "Saw", Barcode = "4820001000016" };
        await _repository.AddAsync(drill);
        await _repository.AddAsync(saw);

        Assert.Same(saw, await _repository.GetByIdAsync(saw.Id));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForUnknownId()
    {
        await _repository.AddAsync(new Product { Name = "Drill", Barcode = "4006381333931" });

        Assert.Null(await _repository.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByBarcodeAsync_ReturnsProductWithExactBarcode()
    {
        var drill = new Product { Name = "Drill", Barcode = "4006381333931" };
        await _repository.AddAsync(drill);

        Assert.Same(drill, await _repository.GetByBarcodeAsync("4006381333931"));
    }

    [Fact]
    public async Task GetByBarcodeAsync_ReturnsNullWhenNoProductHasBarcode()
    {
        await _repository.AddAsync(new Product { Name = "Drill", Barcode = "4006381333931" });

        Assert.Null(await _repository.GetByBarcodeAsync("4820001000016"));
    }

    [Fact]
    public async Task AddAsync_RejectsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddAsync(null!));
    }
}
