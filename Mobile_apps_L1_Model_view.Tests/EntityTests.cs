using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Tests;

public class EntityTests
{
    [Fact]
    public void NewEntity_GetsNonEmptyVersion7Id()
    {
        var product = new Product();

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal(7, product.Id.Version);
    }

    [Fact]
    public void NewEntities_GetDistinctIds()
    {
        Assert.NotEqual(new Product().Id, new Product().Id);
    }

    [Fact]
    public void NewEntity_HasUtcCreatedAtEqualToUpdatedAt()
    {
        var before = DateTime.UtcNow;
        var user = new User();
        var after = DateTime.UtcNow;

        Assert.Equal(DateTimeKind.Utc, user.CreatedAt.Kind);
        Assert.InRange(user.CreatedAt, before, after);
        Assert.Equal(user.CreatedAt, user.UpdatedAt);
    }

    [Fact]
    public void OwnedEntities_RecordCreator()
    {
        var manager = new User { FullName = "Olena Manager", Role = Role.Manager };

        var characteristic = new Characteristic
        {
            Name = "Weight",
            ValueType = CharacteristicValueType.Number,
            CreatedByUserId = manager.Id
        };

        Assert.Equal(manager.Id, characteristic.CreatedByUserId);
        Assert.IsAssignableFrom<OwnedEntity>(characteristic);
        Assert.IsNotAssignableFrom<OwnedEntity>(manager);
    }

    [Fact]
    public void ProductCharacteristic_LinksProductAndCharacteristic()
    {
        var product = new Product { Name = "Laptop", Barcode = "4006381333931" };
        var weight = new Characteristic { Name = "Weight", ValueType = CharacteristicValueType.Number };

        var value = new ProductCharacteristic
        {
            ProductId = product.Id,
            CharacteristicId = weight.Id,
            Value = "1.4"
        };

        Assert.Equal(product.Id, value.ProductId);
        Assert.Equal(weight.Id, value.CharacteristicId);
    }

    [Fact]
    public void Operation_DefaultsToNoCounterpartyOrReason()
    {
        var operation = new Operation { Direction = OperationDirection.Out, Quantity = 2 };

        Assert.Null(operation.Counterparty);
        Assert.Null(operation.Reason);
    }
}
