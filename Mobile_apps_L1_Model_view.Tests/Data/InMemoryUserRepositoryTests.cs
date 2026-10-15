using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Tests.Data;

public class InMemoryUserRepositoryTests
{
    private readonly InMemoryUserRepository _repository = new();

    [Fact]
    public async Task GetAllAsync_IsEmptyForNewRepository()
    {
        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task AddAsync_KeepsInsertionOrder()
    {
        var manager = new User { FullName = "Olena Kovalenko", Role = Role.Manager };
        var storekeeper = new User { FullName = "Petro Bondarenko", Role = Role.Storekeeper };

        await _repository.AddAsync(manager);
        await _repository.AddAsync(storekeeper);

        var users = await _repository.GetAllAsync();
        Assert.Equal([manager.Id, storekeeper.Id], users.Select(u => u.Id).ToArray());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTheUserWithThatId()
    {
        var storekeeper = new User { FullName = "Petro Bondarenko", Role = Role.Storekeeper };
        await _repository.AddAsync(storekeeper);

        Assert.Same(storekeeper, await _repository.GetByIdAsync(storekeeper.Id));
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        await _repository.AddAsync(new User { FullName = "Ivan Shevchenko", Role = Role.Driver });

        Assert.Null(await _repository.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AddAsync_RejectsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddAsync(null!));
    }
}
