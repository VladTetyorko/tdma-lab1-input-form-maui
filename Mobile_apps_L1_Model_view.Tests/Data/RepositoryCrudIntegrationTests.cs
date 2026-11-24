using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Data;

// Lab 8 integration tests: the real repositories over a real SQLite file in the temp folder (AddFakePlatform), one
// whole life of a row, and the SyncQueue row that every write must leave behind (Insert, Update, Delete, in order)
public class RepositoryCrudIntegrationTests : IDisposable
{
    private readonly ServiceProvider _provider = new ServiceCollection().AddStockRoomCore().AddFakePlatform()
        .BuildServiceProvider();

    private IProductRepository Products => _provider.GetRequiredService<IProductRepository>();
    private IProductCharacteristicRepository Values => _provider.GetRequiredService<IProductCharacteristicRepository>();
    private IOperationRepository Operations => _provider.GetRequiredService<IOperationRepository>();
    private ISyncQueue Queue => _provider.GetRequiredService<ISyncQueue>();

    public void Dispose() => _provider.Dispose();

    private async Task ClearQueueAsync()
    {
        foreach (var item in await Queue.GetPendingAsync())
        {
            await Queue.RemoveAsync(item.Id);
        }
    }

    private static Product NewProduct() => new()
    {
        Name = "Drill", Barcode = "4006381333931", CreatedByUserId = SeedData.ManagerId
    };

    [Fact]
    public async Task Product_FullCrud_Save_GetById_Update_Delete()
    {
        var product = NewProduct();

        await Products.AddAsync(product);
        var saved = await Products.GetByIdAsync(product.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Drill");
        saved.Barcode.Should().Be("4006381333931");

        saved.Name = "Hammer drill";
        saved.UpdatedAt = saved.UpdatedAt.AddMinutes(1);
        await Products.UpdateAsync(saved);
        (await Products.GetByIdAsync(product.Id))!.Name.Should().Be("Hammer drill");
        (await Products.GetAllAsync()).Should().ContainSingle(p => p.Id == product.Id);

        await Products.DeleteAsync(product.Id);
        (await Products.GetByIdAsync(product.Id)).Should().BeNull();
        (await Products.GetAllAsync()).Should().NotContain(p => p.Id == product.Id);
    }

    [Fact]
    public async Task Product_EveryWrite_AddsTheExpectedSyncQueueRow()
    {
        await ClearQueueAsync();
        var product = NewProduct();

        await Products.AddAsync(product);
        product.Name = "Hammer drill";
        product.UpdatedAt = product.UpdatedAt.AddMinutes(1);
        await Products.UpdateAsync(product);
        await Products.DeleteAsync(product.Id);

        var queue = await Queue.GetPendingAsync();
        queue.Select(i => i.Operation).Should().Equal(SyncOperation.Insert, SyncOperation.Update, SyncOperation.Delete);
        queue.Should().OnlyContain(i => i.EntityType == "Products" && i.EntityId == product.Id);
        queue[0].PayloadJson.Should().Contain("Drill");
        queue[1].PayloadJson.Should().Contain("Hammer drill");
        queue[2].PayloadJson.Should().Contain("Hammer drill");
    }

    [Fact]
    public async Task ProductCharacteristicValue_FullCrud_AndEveryWriteQueuesARow()
    {
        var product = NewProduct();
        await Products.AddAsync(product);
        await ClearQueueAsync();
        var characteristicId = (await _provider.GetRequiredService<ICharacteristicRepository>().GetAllAsync()).First().Id;
        var value = new ProductCharacteristic
        {
            ProductId = product.Id, CharacteristicId = characteristicId, Value = "red", CreatedByUserId = SeedData.ManagerId
        };

        await Values.AddAsync(value);
        (await Values.GetByProductAsync(product.Id)).Should().ContainSingle(v => v.Value == "red");

        value.Value = "blue";
        value.UpdatedAt = value.UpdatedAt.AddMinutes(1);
        await Values.UpdateAsync(value);
        (await Values.GetByProductAsync(product.Id)).Should().ContainSingle(v => v.Value == "blue");

        await Values.DeleteAsync(value.Id);
        (await Values.GetByProductAsync(product.Id)).Should().BeEmpty();

        var queue = await Queue.GetPendingAsync();
        queue.Select(i => (i.EntityType, i.Operation)).Should().Equal(
            ("ProductCharacteristics", SyncOperation.Insert),
            ("ProductCharacteristics", SyncOperation.Update),
            ("ProductCharacteristics", SyncOperation.Delete));
    }

    [Fact]
    public async Task Operation_IsSavedAndReadBack_AndQueuesOneInsert_ItHasNoUpdateOrDelete()
    {
        var product = NewProduct();
        await Products.AddAsync(product);
        await ClearQueueAsync();
        var operation = new Operation
        {
            ProductId = product.Id, Direction = OperationDirection.In, Quantity = 7, CreatedByUserId = SeedData.ManagerId
        };

        await Operations.AddAsync(operation);

        var stored = (await Operations.GetByProductAsync(product.Id)).Should().ContainSingle().Subject;
        stored.Quantity.Should().Be(7);
        stored.Direction.Should().Be(OperationDirection.In);
        var item = (await Queue.GetPendingAsync()).Should().ContainSingle().Subject;
        item.Operation.Should().Be(SyncOperation.Insert);
        item.EntityType.Should().Be("Operations");
        typeof(IOperationRepository).GetMethods().Select(m => m.Name)
            .Should().NotContain(["UpdateAsync", "DeleteAsync"], "an operation is an immutable record");
    }

    [Fact]
    public async Task FailedWrite_LeavesNoSyncQueueRow()
    {
        await ClearQueueAsync();
        var first = NewProduct();
        await Products.AddAsync(first);
        await ClearQueueAsync();

        // The same barcode twice breaks the unique index: the whole write, including its queue row, is rolled back
        var duplicate = NewProduct();
        var act = () => Products.AddAsync(duplicate);

        await act.Should().ThrowAsync<Exception>();
        (await Queue.GetPendingAsync()).Should().BeEmpty();
        (await Products.GetByIdAsync(duplicate.Id)).Should().BeNull();
    }
}
